using System.Globalization;
using System.Runtime.CompilerServices;
using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Posts;

public sealed class PublishPostHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    IOptions<GameBalance> balance,
    TimeProvider timeProvider,
    GameDay gameDay,
    PlayerReader playerReader,
    ChallengeLifecycle challengeLifecycle,
    PostProgressionCalculator progressionCalculator,
    ProductTelemetry analytics) : IStreamRequestHandler<PublishPostCommand, LiveEvent>
{
    public async IAsyncEnumerable<LiveEvent> Handle(
        PublishPostCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var publication = await store.GetPublicationAsync(request.PublicationId, cancellationToken);
        var storedPlayer = await playerReader.GetAsync(
            telegramUserId,
            cancellationToken);
        if (publication is null || publication.PlayerId != storedPlayer.Id)
        {
            throw new ResourceNotFoundException("Publication not found.");
        }

        if (publication.Status == PublicationStatus.Completed && publication.Result is not null)
        {
            foreach (var liveEvent in PostContractMapper.MapLiveEvents(publication.Result))
            {
                yield return liveEvent;
            }

            yield break;
        }

        if (publication.Status != PublicationStatus.ReadyForModeration)
        {
            throw new RequestConflictException("The publication is not ready for moderation.");
        }

        var text = publication.Text;
        var ownWords = publication.OwnWords;
        var wantId = publication.WantId;
        var now = timeProvider.GetUtcNow();
        var player = gameDay.Normalize(storedPlayer);
        if (!ReferenceEquals(player, storedPlayer) && !await store.TryReplacePlayerAsync(
            player,
            storedPlayer.Version,
            cancellationToken))
        {
            throw new RequestConflictException("The player state changed while starting a new day.");
        }

        var cap = player.GetPostsCap(balance.Value.FreePostsPerDay, balance.Value.PlusPostsPerDay);

        if (player.PostsUsed >= cap)
        {
            throw new RequestConflictException("The daily post limit has been reached.");
        }

        var turn = publication.AssembleTurn();
        StarTurnPolicy.EnsurePostAllowed(turn, player.World, StarTurnPolicy.IsHateEnabled(player, balance.Value, now));

        var wantCompleted = wantId is not null && player.Wants.Any(want =>
            want.Id == wantId && !want.Done);
        if (wantId is not null && !wantCompleted)
        {
            throw new RequestConflictException("The selected daily want is unavailable.");
        }

        var progression = progressionCalculator.Calculate(
            player,
            turn,
            ownWords,
            wantCompleted);
        var updated = player.PublishPost(
            progression.SubscriberDelta,
            progression.Viral,
            progression.Flop,
            progression.Skills,
            wantId,
            turn.StarId,
            now,
            balance.Value.Relations.Minimum,
            balance.Value.Relations.Maximum,
            balance.Value.Relations.MaxGainPerDay,
            balance.Value.Relations.StarCardAt);
        var postId = PostId.New();
        var starComment = new GameComment(
            CommentId.New(),
            turn.StarName,
            turn.StarId.Value,
            $"@{turn.StarId.Value}",
            turn.Comment,
            true,
            false,
            false);
        var recentPosts = await store.GetPostsAsync(
            player.Id,
            20,
            cancellationToken);
        var recentViews = recentPosts.Select(post => post.Views).ToArray();
        var views = Math.Max(48, updated.Subscribers);
        var reachRatio = recentViews.Length == 0 ? 1 : views / GetMedian(recentViews);
        var post = new GamePost(
            postId,
            player.Id,
            text,
            views,
            [starComment],
            [],
            now,
            progression.SubscriberDelta,
            progression.Quality,
            reachRatio,
            progression.Viral,
            progression.Flop);
        var hook = turn.Hook;

        var published = new PublishedPost(post, hook, turn.StarName, turn.Reaction, turn.Reasons);
        var committed = await store.TryCommitPostAsync(
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
        foreach (var liveEvent in PostContractMapper.MapLiveEvents(published))
        {
            yield return liveEvent;
        }
    }

    private static double GetMedian(IReadOnlyList<long> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
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
            after,
            "post_scored",
            new Dictionary<string, string>
            {
                ["brave"] = turn.Brave.ToString("0.###", CultureInfo.InvariantCulture),
                ["humor"] = turn.Humor.ToString("0.###", CultureInfo.InvariantCulture),
                ["topic"] = turn.Topic.ToString("0.###", CultureInfo.InvariantCulture),
                ["quality"] = post.Quality.ToString("0.###", CultureInfo.InvariantCulture),
                ["post_index"] = after.PostsUsed.ToString(CultureInfo.InvariantCulture),
            });

        foreach ((var skillId, var progress) in after.Skills)
        {
            var previousLevel = before.Skills[skillId].Level;
            if (progress.Level > previousLevel)
            {
                analytics.Track(
                    after,
                    "level_up",
                    new Dictionary<string, string>
                    {
                        ["skill"] = skillId.Value,
                        ["level"] = progress.Level.ToString(CultureInfo.InvariantCulture),
                    });
            }
        }

        var earned = after.Achievements.Where(item =>
            item.IsOpen && !before.Achievements.Any(previous =>
                previous.WorldId == item.WorldId && previous.Id == item.Id && previous.IsOpen));
        foreach (var achievement in earned)
        {
            analytics.Track(
                after,
                "card_earned",
                new Dictionary<string, string> { ["card"] = achievement.Id });
        }
    }
}
