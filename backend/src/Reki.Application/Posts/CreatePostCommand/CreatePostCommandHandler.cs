using System.Globalization;
using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Invites;
using Reki.Application.Player;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.CreatePostCommand.Progression;
using Reki.Application.Posts.CreatePostCommand.Score;
using Reki.Application.Posts.ML;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.PostResult;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class CreatePostCommandHandler(
    IUnitOfWork unitOfWork,
    IPlayerRepository players,
    IWorldRepository worlds,
    IPostRepository posts,
    IInvitationRepository invitations,
    IOptionsMonitor<GameBalance> balance,
    PostProgressionCalculator progressionCalculator,
    PostScorer scorer,
    IStarVectorStore stars,
    IRekiContextAccessor contextAccessor,
    PlayerSafetyGuard safety,
    TimeProvider timeProvider,
    ProductTelemetry telemetry,
    IStarFeedScheduler<ScheduleFromPostRequest> feedScheduler) : ICommandHandler<CreatePost, CreatePostResponse>
{
    public async ValueTask<CreatePostResponse> Handle(CreatePost request, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Context;
        var player = await context.Player;
        var world = (await context.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var currentBalance = balance.CurrentValue;
        if (player.Energy < currentBalance.PostEnergyCost)
        {
            throw new RequestConflictException("The daily post limit has been reached.");
        }

        var wantCompleted = request.WantId is not null && world.Player.Wants.Any(want => want.Id == request.WantId && !want.Done);
        if (request.WantId is not null && !wantCompleted)
        {
            throw new RequestConflictException("The selected daily want is unavailable.");
        }

        await safety.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
        var now = timeProvider.GetUtcNow();
        var post = new GamePost(
            PostId.New(now),
            player.Id.ToAuthorId(),
            world.Id,
            request.Text,
            Random.Shared.NextInt64(world.Player.Subscribers + 1),
            [],
            now,
            0,
            0,
            1,
            false,
            false);
        var score = await scorer.ScoreAsync(post, world, currentBalance, cancellationToken);
        var starId = await stars.GetRelevantCommenterAsync(world, post);
        var progression = progressionCalculator.Calculate(
            player, world, score, request.OwnWords, wantCompleted, post.Id.ToString(), score.FlopReason is not null);
        var updatedWorld = world.PublishPost(
            progression.SubscriberDelta,
            progression.Viral,
            progression.Skills,
            request.WantId,
            starId,
            now,
            currentBalance.Relations.Minimum,
            currentBalance.Relations.Maximum,
            currentBalance.Relations.MaxGainPerDay,
            currentBalance.Relations.StarCardAt);
        var updated = player.SpendPost(progression.Skills, currentBalance.PostEnergyCost) with
        {
            HitOwnWorldMilestone = player.HitOwnWorldMilestone || updatedWorld.Player.Subscribers >= currentBalance.OwnWorldSubscribers,
        };
        post = post with
        {
            SubscriberDelta = progression.SubscriberDelta, Quality = progression.Quality, Viral = progression.Viral,
            Flop = progression.Flop, ReactionEmoji = score.Emoji, PredictedAuthorId = starId.ToAuthorId(), FlopReason = score.FlopReason,
        };
        var invite = await invitations.GetPendingInviteAsync(player.Id, cancellationToken);
        var updatedInvite = invite?.UseInviteeBonus(now).UseInviterBonus(now);
        updated = updated with { Energy = updated.Energy + (updatedInvite?.BonusAmount ?? 0) };
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await players.ReplacePlayerAsync(updated, token);
                await worlds.ReplaceWorldAsync(updatedWorld, token);
                if (updatedInvite is not null)
                {
                    var periodStart = updated.PostsPeriodStartedAtUtc;
                    var inviterUses = await invitations.CountInviterBonusesAsync(
                        updatedInvite.InviterId,
                        periodStart,
                        periodStart.AddDays(1),
                        token);
                    if (inviterUses < currentBalance.InviteBonusMaxPerDay)
                    {
                        await players.AddInviteBonusAsync(updatedInvite.InviterId, updatedInvite.BonusAmount, token);
                    }

                    await invitations.UseInviteBonusAsync(updatedInvite, token);
                }

                await posts.InsertPostAsync(post, token);
                return Unit.Value;
            },
            cancellationToken);
        telemetry.TrackWorldAudience(updatedWorld);
        TrackProgression(player, updated, world, updatedWorld, post, score, telemetry);
        await feedScheduler.ScheduleFeedAsync(new ScheduleFromPostRequest(player.Id, world.Id, post.Id));
        var headline = post.Viral ? "Пост залетел" : "Канал растёт";
        var reason = score.FlopReason switch
        {
            "angry_abuse" => "Агрессивные оскорбления",
            "near_copy" => "Похожий пост уже был",
            "sad_tone" => "Грустный тон",
            "anxious_tone" => "Тревожный тон",
            _ => null,
        };
        var whys = new List<WhyResponse>();
        if (reason is not null)
        {
            whys.Add(new WhyResponse("🤔", reason, post.SubscriberDelta.ToString(CultureInfo.InvariantCulture)));
        }

        if (score.Topic > 0)
        {
            whys.Add(new WhyResponse("🎯", "В тему", string.Empty));
        }

        var result = new PostResultResponse(
            post.Id,
            post.SubscriberDelta,
            post.Flop ? "Аудитория сократилась" : headline,
            whys,
            post.Viral ? [new RibbonResponse("🚀", "залёт", null)] : [],
            null,
            post.Viral,
            post.Flop,
            post.Views);
        return new CreatePostResponse(post.MapPost(updatedWorld, player.Id), result, starId.ToAuthorId());
    }

    private static void TrackProgression(
        PlayerProfile before,
        PlayerProfile after,
        World beforeWorld,
        World afterWorld,
        GamePost post,
        PostScore turn,
        ProductTelemetry analytics)
    {
        analytics.TrackPlayer(
            after,
            "post_scored",
            new Dictionary<string, string>
            {
                ["template_id"] = afterWorld.TemplateId?.ToString() ?? string.Empty,
                ["difficulty"] = afterWorld.Difficulty.ToString().ToLowerInvariant(),
                ["brave"] = turn.Brave.ToString("0.###", CultureInfo.InvariantCulture),
                ["humor"] = turn.Humor.ToString("0.###", CultureInfo.InvariantCulture),
                ["topic"] = turn.Topic.ToString("0.###", CultureInfo.InvariantCulture),
                ["quality"] = post.Quality.ToString("0.###", CultureInfo.InvariantCulture),
                ["post_index"] = after.EnergyUsed.ToString(CultureInfo.InvariantCulture),
                ["id"] = post.Id.Value.ToString("N"),
                ["delta"] = post.SubscriberDelta.ToString(CultureInfo.InvariantCulture),
            });

        foreach (var (skillId, progress) in after.Skills)
        {
            var previousLevel = before.Skills[skillId].Level;
            if (progress.Level > previousLevel)
            {
                analytics.TrackPlayer(
                    after,
                    "level_up",
                    new Dictionary<string, string>
                    {
                        ["template_id"] = afterWorld.TemplateId?.ToString() ?? string.Empty, ["difficulty"] = afterWorld.Difficulty.ToString().ToLowerInvariant(),
                        ["skill"] = skillId.Value,
                        ["level"] = progress.Level.ToString(CultureInfo.InvariantCulture),
                    });
            }
        }

        var earned = afterWorld.Player.Achievements.Where(item =>
            item.IsOpen && !beforeWorld.Player.Achievements.Any(previous =>
                previous.Id == item.Id && previous.IsOpen));
        foreach (var achievement in earned)
        {
            analytics.TrackPlayer(
                after,
                "card_earned",
                new Dictionary<string, string> { ["card"] = achievement.Id, ["template_id"] = afterWorld.TemplateId?.ToString() ?? string.Empty, ["difficulty"] = afterWorld.Difficulty.ToString().ToLowerInvariant() });
        }
    }
}
