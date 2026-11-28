using System.Globalization;
using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Player;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.CreatePostCommand.Progression;
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
    IWorldPlayerCache worldCache,
    IPostRepository posts,
    IOptionsMonitor<GameBalance> balance,
    PostProgressionCalculator progressionCalculator,
    PostCrystalsCalculator postCrystalsCalculator,
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
        var post = new GamePost(
            PostId.New(now),
            player.Id.ToAuthorId(),
            world.Id,
            player.Id,
            command.Text,
            0,
            0,
            [],
            [],
            now,
            0,
            0,
            1,
            PostViralityStatus.None,
            repostId: command.RepostId);

        var previousSkillLevels = player.Skills.ToDictionary(
            item => item.Key,
            item => item.Value.Level);
        HashSet<string> previousAchievements =
        [
            .. world.Player.Achievements
                .Where(item => item.IsOpen)
                .Select(item => item.Id),
        ];

        var progression = await unitOfWork.ExecuteAsync(
            async token =>
            {
                var progression = await progressionCalculator.CalculateAsync(player, world, post, command.OwnWords, token);
                post.ApplyProgress(progression);
                world.PublishPost(progression);

                var hitOwnMilestone = player.HitOwnWorldMilestone || world.Player.Subscribers >= currentBalance.OwnWorldSubscribers;
                player.SpendPost(progression.Skills, currentBalance.PostEnergyCost, now, hitOwnMilestone);

                await worlds.ReplaceWorldAsync(world, token);
                await posts.InsertPostAsync(post, token);

                var publicationNumber = await posts.CountPostsAsync(player.Id, token);
                var crystals = postCrystalsCalculator.GetBonus(post, publicationNumber, progression.LevelChange);
                player.ReplenishCrystals(crystals);
                await players.ReplacePlayerAsync(player, token);

                return progression;
            },
            _ => worldCache.UpdateWorld(world),
            cancellationToken);

        telemetry.TrackWorldAudience(world);
        TrackProgression(previousSkillLevels, previousAchievements, player, world, post, progression.Score, telemetry);
        await feedScheduler.ScheduleFeedAsync(new ScheduleFromPostRequest(player.Id, world.Id, post.Id));

        var reason = progression.Score.FlopReason switch
        {
            "angry_abuse" => "Агрессивные оскорбления",
            "near_copy" => "Похожий пост уже был",
            "sad_tone" => "Грустный тон",
            "anxious_tone" => "Тревожный тон",
            _ => null,
        };

        List<WhyResponse> whys =
        [
            .. reason is null
                ? Array.Empty<WhyResponse>()
                : [new WhyResponse("🤔", reason, post.SubscriberDelta.ToString(CultureInfo.InvariantCulture))],
            progression.Score.Topic > 0
                ? new WhyResponse("🎯", "В тему", string.Empty)
                : new WhyResponse("👎", "Не в тему", string.Empty),
        ];

        var postResultResponse = new PostResultResponse(
            post.Id,
            post.SubscriberDelta,
            post.Virality switch
            {
                PostViralityStatus.Flop => "Аудитория сократилась",
                PostViralityStatus.Viral => "Пост залетел",
                PostViralityStatus.FlopGuard => "Сработала защита от отписок",
                PostViralityStatus.ViralCard => "Сработал бонус залёта",
                _ => "Канал растёт",
            },
            whys,
            RibbonResponse.Create(post.Virality),
            null,
            post.Virality,
            post.Views,
            progression.BoosterEffect is { } effect
                ? new BoosterEffectResponse(
                    effect.Multiplier,
                    effect.ExtraViews,
                    effect.SubscriberDeltaWithoutBooster,
                    effect.ViewsWithoutBooster)
                : null);

        return new CreatePostResponse(
            post.MapPost(world, player.Id),
            postResultResponse,
            progression.Commenter.ToAuthorId());
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
}
