using System.Globalization;
using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed class PublishPostHandler(
    IGameStore store,
    GameBalance balance,
    TimeProvider timeProvider,
    GameDay gameDay,
    PlayerReader playerReader,
    PlayerSafetyGuard safetyGuard,
    ChallengeLifecycle challengeLifecycle,
    PostProgressionCalculator progressionCalculator,
    ServerAnalyticsWriter analytics,
    StarTurnGenerator turnGenerator) : IRequestHandler<PublishPostCommand, PublishedPost>
{
    public async ValueTask<PublishedPost> Handle(
        PublishPostCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile storedPlayer = await playerReader.GetAsync(
            request.TelegramUserId,
            cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();
        PlayerProfile player = gameDay.Normalize(storedPlayer);
        if (!ReferenceEquals(player, storedPlayer) && !await store.TryReplacePlayerAsync(
            player,
            storedPlayer.Version,
            cancellationToken))
        {
            throw new RequestConflictException("The player state changed while starting a new day.");
        }

        int cap = player.GetPostsCap(balance.FreePostsPerDay, balance.PlusPostsPerDay);

        if (player.PostsUsed >= cap)
        {
            throw new RequestConflictException("The daily post limit has been reached.");
        }

        await safetyGuard.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
        WorldRules? storedRules = await store.GetWorldRulesAsync(
            player.Id,
            player.World,
            cancellationToken);
        WorldRules worldRules = storedRules ?? WorldRulesDefaults.Create(player.Id, player.World);
        IReadOnlyList<GamePost> recentPosts = await store.GetRecentPostsAsync(
            player.Id,
            3,
            cancellationToken);
        bool hateEnabled = player.Settings.Drama != "off" &&
            (player.HateDisabledUntilUtc is null || player.HateDisabledUntilUtc <= now);
        GeneratedPostTurn turn = await turnGenerator.GeneratePostTurnAsync(
            new StarPostContext(
                request.Text,
                new StarAgentProfile(
                    player.World,
                    worldRules.Title,
                    worldRules.Description,
                    worldRules.Rules,
                    player.Settings.Drama,
                    player.Settings.Voice,
                    null,
                    []),
                GetStage(player.Subscribers),
                recentPosts.Select(post => post.Text).ToArray(),
                hateEnabled),
            cancellationToken);

        bool wantCompleted = request.WantId is not null && player.Wants.Any(want =>
            want.Id == request.WantId && !want.Done);
        if (request.WantId is not null && !wantCompleted)
        {
            throw new RequestConflictException("The selected daily want is unavailable.");
        }

        PostProgression progression = progressionCalculator.Calculate(
            player,
            turn,
            request.OwnWords,
            wantCompleted);
        PlayerProfile updated = player.PublishPost(
            progression.SubscriberDelta,
            progression.Viral,
            progression.Flop,
            progression.Skills,
            request.WantId,
            turn.StarId,
            now,
            balance.Relations.Minimum,
            balance.Relations.Maximum,
            balance.Relations.MaxGainPerDay,
            balance.Relations.StarCardAt);
        bool replaced = await store.TryReplacePlayerAsync(
            updated,
            player.Version,
            cancellationToken);
        if (!replaced)
        {
            throw new RequestConflictException("The player state changed while publishing the post.");
        }

        PostId postId = PostId.New();
        var starComment = new GameComment(
            CommentId.New(),
            turn.StarName,
            turn.StarId.Value,
            $"@{turn.StarId.Value}",
            turn.Comment,
            true,
            false,
            false);
        IReadOnlyList<long> recentViews = await store.GetRecentPostViewsAsync(
            player.Id,
            20,
            cancellationToken);
        long views = Math.Max(48, updated.Subscribers);
        double reachRatio = recentViews.Count == 0 ? 1 : views / GetMedian(recentViews);
        var post = new GamePost(
            postId,
            player.Id,
            request.Text,
            views,
            [starComment],
            now,
            progression.SubscriberDelta,
            progression.Quality,
            reachRatio,
            progression.Viral,
            progression.Flop);
        string hook = turn.Hook;

        await store.AddPostAsync(post, cancellationToken);
        await challengeLifecycle.RecordPostAsync(player.Id, post, cancellationToken);
        await TrackProgressionAsync(player, updated, post, turn, analytics, cancellationToken);
        await store.AppendThreadAsync(player.Id, postId, hook, now, cancellationToken);
        return new PublishedPost(post, hook, turn.StarName, turn.Reaction, turn.Reasons);
    }

    private static double GetMedian(IReadOnlyList<long> values)
    {
        long[] ordered = values.Order().ToArray();
        int middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? Math.Max(1, (ordered[middle - 1] + ordered[middle]) / 2.0)
            : Math.Max(1, ordered[middle]);
    }

    private static string GetStage(long subscribers)
    {
        return subscribers switch
        {
            < 1000 => "mask",
            < 5000 => "views",
            < 10000 => "strike",
            _ => "new_line",
        };
    }

    private static async ValueTask TrackProgressionAsync(
        PlayerProfile before,
        PlayerProfile after,
        GamePost post,
        GeneratedPostTurn turn,
        ServerAnalyticsWriter analytics,
        CancellationToken cancellationToken)
    {
        await analytics.TrackAsync(
            after.Id,
            "post_scored",
            new Dictionary<string, string>
            {
                ["brave"] = turn.Brave.ToString("0.###", CultureInfo.InvariantCulture),
                ["humor"] = turn.Humor.ToString("0.###", CultureInfo.InvariantCulture),
                ["topic"] = turn.Topic.ToString("0.###", CultureInfo.InvariantCulture),
                ["quality"] = post.Quality.ToString("0.###", CultureInfo.InvariantCulture),
                ["post_index"] = after.PostsUsed.ToString(CultureInfo.InvariantCulture),
            },
            cancellationToken);

        foreach ((SkillId skillId, SkillProgress progress) in after.Skills)
        {
            int previousLevel = before.Skills[skillId].Level;
            if (progress.Level > previousLevel)
            {
                await analytics.TrackAsync(
                    after.Id,
                    "level_up",
                    new Dictionary<string, string>
                    {
                        ["skill"] = skillId.Value,
                        ["level"] = progress.Level.ToString(CultureInfo.InvariantCulture),
                    },
                    cancellationToken);
            }
        }

        IEnumerable<AchievementProgress> earned = after.Achievements.Where(item =>
            item.IsOpen && !before.Achievements.Any(previous =>
                previous.WorldId == item.WorldId && previous.Id == item.Id && previous.IsOpen));
        foreach (AchievementProgress achievement in earned)
        {
            await analytics.TrackAsync(
                after.Id,
                "card_earned",
                new Dictionary<string, string> { ["card"] = achievement.Id },
                cancellationToken);
        }
    }
}
