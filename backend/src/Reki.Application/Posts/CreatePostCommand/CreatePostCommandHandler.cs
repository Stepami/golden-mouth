using System.Globalization;
using Mediator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Bonuses;
using Reki.Application.Exceptions;
using Reki.Application.Invites;
using Reki.Application.Player;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.CreatePostCommand.Progression;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.PostResult;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Domain.Bonuses;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class CreatePostCommandHandler(
    IUnitOfWork unitOfWork,
    IPlayerRepository players,
    IWorldRepository worlds,
    IWorldPlayerCache worldCache,
    IPostRepository posts,
    IInvitationRepository invitationRepository,
    IBonusRepository bonuses,
    IOptionsMonitor<GameBalance> balance,
    PostProgressionCalculator progressionCalculator,
    IRekiEmbeddingGenerator<GamePost> postEmbedder,
    IRekiContextAccessor contextAccessor,
    PlayerSafetyGuard safety,
    TimeProvider timeProvider,
    ProductTelemetry telemetry,
    IStarFeedScheduler<ScheduleFromPostRequest> feedScheduler) : ICommandHandler<CreatePost, CreatePostResponse>
{
    public async ValueTask<CreatePostResponse> Handle(CreatePost command, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Context;
        var player = await context.Player;
        var playerId = player.Id;
        var world = (await context.Worlds).GetValueOrDefault(command.WorldId)
                    ?? throw new ResourceNotFoundException("World not found.");
        if (command.RepostId is null && string.IsNullOrWhiteSpace(command.Text))
        {
            throw new RequestConflictException("Post text is required.");
        }

        var currentBalance = balance.CurrentValue;
        if (player.Wallet.Energy < currentBalance.PostEnergyCost)
        {
            throw new RequestConflictException("Not enough energy to publish a post.");
        }

        await safety.EnsureInputAllowedAsync(player, [command.Text], cancellationToken);
        var now = timeProvider.GetUtcNow();
        var postId = PostId.New(now);
        var draft = CreateDraft();
        var (score, commenter) = await progressionCalculator.ScoreAsync(world, draft, command.OwnWords);
        var embedding = await postEmbedder.GenerateVectorAsync(draft, cancellationToken: cancellationToken);

        var (post, progression, updated, updatedWorld, previousSkillLevels, previousAchievements) = await unitOfWork.ExecuteAsync(
            async token =>
            {
                var currentPlayer = await players.GetPlayerAsync(playerId, token)
                                    ?? throw new ResourceNotFoundException("Player not found.");
                var currentWorld = await worlds.GetWorldAsync(playerId, world.Id, token)
                                   ?? throw new ResourceNotFoundException("World not found.");
                if (currentPlayer.Wallet.Energy < currentBalance.PostEnergyCost)
                {
                    throw new RequestConflictException("Not enough energy to publish a post.");
                }

                var savedPost = CreateDraft();
                var outcome = PostProgressionCalculator.GetOutcome(currentWorld, savedPost, score, currentBalance);
                var bonus = !outcome.IsRepost && !outcome.Viral
                    ? await ClaimPostBonusAsync(playerId, outcome.Flop, now, token)
                    : null;
                var appliedProgression = PostProgressionCalculator.Calculate(
                    currentPlayer, currentWorld, command.OwnWords, outcome, score, commenter, bonus, currentBalance);
                savedPost.ApplyProgress(appliedProgression);

                var skillLevelsBefore = currentPlayer.Skills.ToDictionary(item => item.Key, item => item.Value.Level);
                var achievementsBefore = currentWorld.Player.Achievements.Where(item => item.IsOpen)
                    .Select(item => item.Id).ToHashSet();
                var publishedWorld = currentWorld.PublishPost(appliedProgression);
                var hitOwnMilestone = currentPlayer.HitOwnWorldMilestone ||
                                      publishedWorld.Player.Subscribers >= currentBalance.OwnWorldSubscribers;
                var updatedPlayer = currentPlayer.SpendPost(
                    appliedProgression.Skills, currentBalance.PostEnergyCost, now, hitOwnMilestone);

                var invite = await invitationRepository.GetPendingInviteAsync(playerId, token);
                var updatedInvite = invite?.UseInviteeBonus(now).UseInviterBonus(now);
                updatedPlayer.ReplenishCrystals(updatedInvite?.BonusAmount ?? 0);
                await players.ReplacePlayerAsync(updatedPlayer, token);
                await worlds.ReplaceWorldAsync(publishedWorld, token);
                if (updatedInvite is not null)
                {
                    var inviter = await players.GetPlayerAsync(updatedInvite.InviterId, token);
                    if (inviter is not null)
                    {
                        await players.ReplacePlayerAsync(inviter.AwardReferral(), token);
                    }

                    await invitationRepository.UseInviteBonusAsync(updatedInvite, token);
                }

                await posts.InsertPostAsync(savedPost, embedding, token);
                return (Post: savedPost, Progression: appliedProgression, Player: updatedPlayer, World: publishedWorld,
                    PreviousSkillLevels: skillLevelsBefore, PreviousAchievements: achievementsBefore);
            },
            publication => worldCache.UpdateWorld(publication.World),
            cancellationToken);
        telemetry.TrackWorldAudience(updatedWorld);
        TrackProgression(previousSkillLevels, previousAchievements, updated, updatedWorld, post, progression.Score, telemetry);
        await feedScheduler.ScheduleFeedAsync(new ScheduleFromPostRequest(playerId, world.Id, post.Id));
        var headline = post.Viral ? "Пост залетел" : "Канал растёт";
        var reason = progression.Score.FlopReason switch
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

        whys.Add(
            progression.Score.Topic > 0
                ? new WhyResponse("🎯", "В тему", string.Empty)
                : new WhyResponse("👎", "Не в тему", string.Empty));

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

        return new CreatePostResponse(
            post.MapPost(updatedWorld, playerId),
            result,
            progression.Commenter.ToAuthorId());

        GamePost CreateDraft() => new(
            postId,
            playerId.ToAuthorId(),
            world.Id,
            playerId,
            command.Text,
            0,
            0,
            [],
            [],
            now,
            0,
            0,
            1,
            false,
            false,
            repostId: command.RepostId);
    }

    private static void TrackProgression(
        IReadOnlyDictionary<SkillId, int> previousSkillLevels,
        IReadOnlySet<string> previousAchievements,
        PlayerProfile after,
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
                ["id"] = post.Id.Value.ToString("N"),
                ["publication_kind"] =
                    post switch { { RepostId: null } => "post", { Text: "" } => "repost", _ => "quote" },
                ["delta"] = post.SubscriberDelta.ToString(CultureInfo.InvariantCulture),
            });

        foreach (var (skillId, progress) in after.Skills)
        {
            var previousLevel = previousSkillLevels[skillId];
            if (progress.Level > previousLevel)
            {
                analytics.TrackPlayer(
                    after,
                    "level_up",
                    new Dictionary<string, string>
                    {
                        ["template_id"] = afterWorld.TemplateId?.ToString() ?? string.Empty,
                        ["difficulty"] = afterWorld.Difficulty.ToString().ToLowerInvariant(),
                        ["skill"] = skillId.Value,
                        ["level"] = progress.Level.ToString(CultureInfo.InvariantCulture),
                    });
            }
        }

        var earned = afterWorld.Player.Achievements.Where(item =>
            item.IsOpen && !previousAchievements.Contains(item.Id));
        foreach (var achievement in earned)
        {
            analytics.TrackPlayer(
                after,
                "card_earned",
                new Dictionary<string, string>
                {
                    ["card"] = achievement.Id, ["template_id"] = afterWorld.TemplateId?.ToString() ?? string.Empty,
                    ["difficulty"] = afterWorld.Difficulty.ToString().ToLowerInvariant(),
                });
        }
    }

    private async Task<Bonus?> ClaimPostBonusAsync(
        PlayerId playerId,
        bool flop,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        Bonus? card = flop
            ? await bonuses.GetActiveBonus<FlopGuardBonus>(playerId, cancellationToken)
            : await bonuses.GetActiveBonus<ViralCardBonus>(playerId, cancellationToken);
        if (card is null)
        {
            return await GetActiveBoosterAsync(playerId, now, cancellationToken);
        }

        card.Claim();
        await bonuses.SaveBonus(card, cancellationToken);
        return card;
    }

    private async Task<PostBoosterBonus?> GetActiveBoosterAsync(
        PlayerId playerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        while (await bonuses.GetActiveBonus<PostBoosterBonus>(playerId, cancellationToken) is { } booster)
        {
            booster.Claim(now);
            if (!booster.IsClaimed)
            {
                return booster;
            }

            await bonuses.SaveBonus(booster, cancellationToken);
        }

        return null;
    }
}
