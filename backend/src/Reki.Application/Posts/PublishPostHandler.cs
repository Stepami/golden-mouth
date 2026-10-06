using System.Globalization;
using System.Runtime.CompilerServices;
using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class PublishPostHandler(
    IGameStore store,
    GameBalance balance,
    TimeProvider timeProvider,
    GameDay gameDay,
    PlayerReader playerReader,
    ChallengeLifecycle challengeLifecycle,
    PostProgressionCalculator progressionCalculator,
    ProductTelemetry analytics) : IStreamRequestHandler<PublishPostCommand, object>
{
    public async IAsyncEnumerable<object> Handle(
        PublishPostCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Publication? publication = await store.GetPublicationAsync(request.PublicationId, cancellationToken);
        PlayerProfile storedPlayer = await playerReader.GetAsync(
            request.TelegramUserId,
            cancellationToken);
        if (publication is null || publication.PlayerId != storedPlayer.Id)
        {
            throw new ResourceNotFoundException("Publication not found.");
        }

        if (publication.Status == PublicationStatus.Completed && publication.Result is not null)
        {
            foreach (object liveEvent in PostContractMapper.MapLiveEvents(publication.Result))
            {
                yield return liveEvent;
            }

            yield break;
        }

        if (publication.Status != PublicationStatus.ReadyForModeration)
        {
            throw new RequestConflictException("The publication is not ready for moderation.");
        }

        string text = publication.Text;
        bool ownWords = publication.OwnWords;
        string? wantId = publication.WantId;
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

        GeneratedPostTurn turn = publication.AssembleTurn();

        bool wantCompleted = wantId is not null && player.Wants.Any(want =>
            want.Id == wantId && !want.Done);
        if (wantId is not null && !wantCompleted)
        {
            throw new RequestConflictException("The selected daily want is unavailable.");
        }

        PostProgression progression = progressionCalculator.Calculate(
            player,
            turn,
            ownWords,
            wantCompleted);
        PlayerProfile updated = player.PublishPost(
            progression.SubscriberDelta,
            progression.Viral,
            progression.Flop,
            progression.Skills,
            wantId,
            turn.StarId,
            now,
            balance.Relations.Minimum,
            balance.Relations.Maximum,
            balance.Relations.MaxGainPerDay,
            balance.Relations.StarCardAt);
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
            text,
            views,
            [starComment],
            now,
            progression.SubscriberDelta,
            progression.Quality,
            reachRatio,
            progression.Viral,
            progression.Flop);
        string hook = turn.Hook;

        var published = new PublishedPost(post, hook, turn.StarName, turn.Reaction, turn.Reasons);
        bool committed = await store.TryCommitPostAsync(
            updated,
            player.Version,
            published,
            request.PublicationId,
            cancellationToken);
        if (!committed)
        {
            throw new RequestConflictException("The player state changed while publishing the post.");
        }

        await challengeLifecycle.RecordPostAsync(player.Id, post, cancellationToken);
        TrackProgression(player, updated, post, turn, analytics);
        foreach (object liveEvent in PostContractMapper.MapLiveEvents(published))
        {
            yield return liveEvent;
        }
    }

    private static double GetMedian(IReadOnlyList<long> values)
    {
        long[] ordered = values.Order().ToArray();
        int middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? Math.Max(1, (ordered[middle - 1] + ordered[middle]) / 2.0)
            : Math.Max(1, ordered[middle]);
    }

    private static void TrackProgression(
        PlayerProfile before,
        PlayerProfile after,
        GamePost post,
        GeneratedPostTurn turn,
        ProductTelemetry analytics)
    {
        analytics.Track(
            after.Id,
            "post_scored",
            new Dictionary<string, string>
            {
                ["brave"] = turn.Brave.ToString("0.###", CultureInfo.InvariantCulture),
                ["humor"] = turn.Humor.ToString("0.###", CultureInfo.InvariantCulture),
                ["topic"] = turn.Topic.ToString("0.###", CultureInfo.InvariantCulture),
                ["quality"] = post.Quality.ToString("0.###", CultureInfo.InvariantCulture),
                ["post_index"] = after.PostsUsed.ToString(CultureInfo.InvariantCulture),
            });

        foreach ((SkillId skillId, SkillProgress progress) in after.Skills)
        {
            int previousLevel = before.Skills[skillId].Level;
            if (progress.Level > previousLevel)
            {
                analytics.Track(
                    after.Id,
                    "level_up",
                    new Dictionary<string, string>
                    {
                        ["skill"] = skillId.Value,
                        ["level"] = progress.Level.ToString(CultureInfo.InvariantCulture),
                    });
            }
        }

        IEnumerable<AchievementProgress> earned = after.Achievements.Where(item =>
            item.IsOpen && !before.Achievements.Any(previous =>
                previous.WorldId == item.WorldId && previous.Id == item.Id && previous.IsOpen));
        foreach (AchievementProgress achievement in earned)
        {
            analytics.Track(
                after.Id,
                "card_earned",
                new Dictionary<string, string> { ["card"] = achievement.Id });
        }
    }
}
