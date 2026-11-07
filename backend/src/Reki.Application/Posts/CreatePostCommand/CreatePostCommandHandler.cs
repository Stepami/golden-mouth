using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Invites;
using Reki.Application.Player;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.ML;
using Reki.Application.Posts.PublishPostCommand;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class CreatePostCommandHandler(
    IRekiContextAccessor accessor,
    IPostRepository posts,
    IPlayerRepository players,
    IWorldRepository worlds,
    IInvitationRepository invitations,
    IUnitOfWork unitOfWork,
    IOptionsMonitor<GameBalance> balance,
    PostProgressionCalculator calculator,
    INliScorer nli,
    IClassifier classifier,
    PlayerSafetyGuard safety,
    TimeProvider timeProvider,
    ProductTelemetry telemetry,
    IStarFeedScheduler<ScheduleFromPostRequest> feedScheduler) : ICommandHandler<CreatePost, PostDetailResponse>
{
    public async ValueTask<PostDetailResponse> Handle(CreatePost request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.RequestId, out var requestGuid))
        {
            throw new RequestConflictException("The request identifier is invalid.");
        }

        var context = accessor.Context;
        var player = await context.Player;
        var world = (await context.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var requestId = requestGuid.ToString("N");
        var existing = await posts.GetByRequestAsync(world.Id, player.Id.ToAuthorId(), requestId, cancellationToken);
        if (existing is not null)
        {
            return Replay(existing);
        }

        await safety.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
        var current = balance.CurrentValue;
        if (player.Energy < current.PostEnergyCost)
        {
            throw new RequestConflictException("Insufficient energy.");
        }

        var wantCompleted = request.WantId is not null && world.Player.Wants.Any(want => want.Id == request.WantId && !want.Done);
        if (request.WantId is not null && !wantCompleted)
        {
            throw new RequestConflictException("The selected daily want is unavailable.");
        }

        var scores = await nli.ScoreAsync(request.Text, [$"В мире {world.Interface.Title} автор смело высказывает своё мнение.", $"В мире {world.Interface.Title} текст содержит юмор.", $"Текст посвящён теме {world.Interface.Title}: {world.Interface.Description}"], cancellationToken);
        var ownText = Regex.Replace(request.Text, "«[^»]*»|\"[^\"]*\"", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1));
        var toxicity = await classifier.ClassifyAsync(ClassifierModels.ToxicityModel, ownText, cancellationToken);
        var emotion = await classifier.ClassifyAsync(ClassifierModels.EmotionModel, request.Text, cancellationToken);
        var ownEmotion = ownText == request.Text ? emotion : await classifier.ClassifyAsync(ClassifierModels.EmotionModel, ownText, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var postId = PostId.New(now);
        try
        {
            var committed = await unitOfWork.ExecuteAsync(
                async token =>
                {
                    var latestPlayer = await players.GetPlayerAsync(player.Id, token)
                        ?? throw new ResourceNotFoundException("Player not found.");
                    var latestWorld = await worlds.GetWorldAsync(latestPlayer.Id, world.Id, token)
                        ?? throw new ResourceNotFoundException("World not found.");
                    player = latestPlayer;
                    world = latestWorld;
                    current = balance.CurrentValue;
                    if (player.Energy < current.PostEnergyCost)
                    {
                        throw new RequestConflictException("Insufficient energy.");
                    }

                    wantCompleted = request.WantId is not null && world.Player.Wants.Any(want => want.Id == request.WantId && !want.Done);
                    if (request.WantId is not null && !wantCompleted)
                    {
                        throw new RequestConflictException("The selected daily want is unavailable.");
                    }

                    var profile = current.Difficulties[world.Difficulty];
                    var drama = profile.Drama?.GetValueOrDefault(world.Player.Drama);
                    var steps = drama?.ThresholdSteps ?? 0;
                    var abuse = Math.Min(toxicity["insult"], ownEmotion["anger"]);
                    var sad = emotion["sadness"];
                    var anxious = emotion["fear"];
                    string? reason = null;
                    if (abuse >= profile.AngryAbuse - (steps * profile.AngryStep))
                    {
                        reason = "angry_abuse";
                    }
                    else if (sad >= profile.SadTone - (steps * profile.SadStep) && (sad >= anxious || anxious < profile.AnxiousTone - (steps * profile.AnxiousStep)))
                    {
                        reason = "sad_tone";
                    }
                    else if (anxious >= profile.AnxiousTone - (steps * profile.AnxiousStep))
                    {
                        reason = "anxious_tone";
                    }
                    else
                    {
                        reason = null;
                    }

                    var star = world.Stars[(int)(BitConverter.ToUInt32(SHA256.HashData(postId.Value.ToByteArray()), 0) % (uint)world.Stars.Count)];
                    var postScores = new PostScores(scores[0].Entailment, scores[1].Entailment, scores[2].Entailment);
                    var progression = calculator.Calculate(player, world, postScores, request.OwnWords, wantCompleted, requestId, reason is not null);
                    var updatedWorld = world.PublishPost(progression.SubscriberDelta, progression.Viral, progression.Skills, request.WantId, star.Id, now, current.Relations.Minimum, current.Relations.Maximum, current.Relations.MaxGainPerDay, current.Relations.StarCardAt);
                    var updatedPlayer = player.SpendPost(progression.Skills, current.PostEnergyCost) with { HitOwnWorldMilestone = player.HitOwnWorldMilestone || updatedWorld.Player.Subscribers >= current.OwnWorldSubscribers };
                    var views = (long)RandomNumberGenerator.GetInt32(0, (int)Math.Min(int.MaxValue - 1, world.Player.Subscribers) + 1);
                    var post = new GamePost(postId, player.Id.ToAuthorId(), world.Id, request.Text, views, [], now, updatedWorld.Player.Subscribers - world.Player.Subscribers, progression.Quality, 1, progression.Viral, progression.Flop)
                    {
                        RequestId = requestId,
                        OwnWords = request.OwnWords,
                        WantId = request.WantId,
                        PredictedAuthorId = star.Id.ToAuthorId(),
                        Reason = reason ?? MainSkill(postScores),
                        ReactionEmoji = scores[1].Entailment > scores[0].Entailment && scores[1].Entailment >= scores[2].Entailment ? "😂" : "🔥",
                    };
                    var invite = await invitations.GetPendingInviteAsync(player.Id, token);
                    var updatedInvite = invite?.UseInviteeBonus(now).UseInviterBonus(now);
                    updatedPlayer = updatedPlayer with { Energy = updatedPlayer.Energy + (updatedInvite?.BonusAmount ?? 0) };
                    await posts.InsertPostAsync(post, token);
                    await players.ReplacePlayerAsync(updatedPlayer, token);
                    await worlds.ReplaceWorldAsync(updatedWorld, token);
                    if (updatedInvite is not null)
                    {
                        var start = updatedPlayer.PostsPeriodStartedAtUtc;
                        var count = await invitations.CountInviterBonusesAsync(updatedInvite.InviterId, start, start.AddDays(1), token);
                        if (count < current.InviteBonusMaxPerDay)
                        {
                            await players.AddInviteBonusAsync(updatedInvite.InviterId, updatedInvite.BonusAmount, token);
                        }

                        await invitations.UseInviteBonusAsync(updatedInvite, token);
                    }

                    return (Before: player, After: updatedPlayer, WorldBefore: world, WorldAfter: updatedWorld, Post: post, Scores: postScores);
                },
                cancellationToken);
            telemetry.TrackWorldAudience(committed.WorldAfter);
            PublishPostCommandHandler.TrackProgression(committed.Before, committed.After, committed.WorldBefore, committed.WorldAfter, committed.Post, committed.Scores, telemetry);
            await feedScheduler.ScheduleFeedAsync(new ScheduleFromPostRequest(committed.Before.Id, committed.WorldBefore.Id, committed.Post.Id));
            return committed.Post.MapPost(committed.WorldAfter, committed.Before.Id);
        }
        catch (UnitOfWorkException)
        {
            existing = await posts.GetByRequestAsync(world.Id, player.Id.ToAuthorId(), requestId, cancellationToken);
            if (existing is null)
            {
                throw;
            }

            return Replay(existing);
        }

        PostDetailResponse Replay(GamePost saved)
        {
            if (saved.Text != request.Text || saved.OwnWords != request.OwnWords || saved.WantId != request.WantId)
            {
                throw new RequestConflictException("The request identifier belongs to another post.");
            }

            return saved.MapPost(world, player.Id);
        }
    }

    private static string MainSkill(PostScores scores)
    {
        if (scores.Brave >= scores.Humor && scores.Brave >= scores.Topic)
        {
            return "brave";
        }

        return scores.Humor >= scores.Topic ? "humor" : "topic";
    }
}
