using System.Globalization;
using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Player;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class PublishPostHandler(
    IGameStore store,
    IOptions<GameBalance> balance,
    TimeProvider timeProvider,
    GameDay gameDay,
    ISender sender,
    ChallengeLifecycle challengeLifecycle,
    PostProgressionCalculator progressionCalculator,
    ProductTelemetry analytics) : IRequestHandler<PublishPostCommand, IReadOnlyList<LiveEvent>>
{
    public async ValueTask<IReadOnlyList<LiveEvent>> Handle(
        PublishPostCommand request,
        CancellationToken cancellationToken)
    {
        var publication = await sender.Send(new GetPublicationQuery(request.PublicationId), cancellationToken);
        var storedPlayer = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var world = await sender.Send(new Worlds.GetWorldQuery(storedPlayer.Id, request.WorldId), cancellationToken);
        if (publication is null || publication.PlayerId != storedPlayer.Id || publication.WorldId != world.Id)
        {
            throw new ResourceNotFoundException("Publication not found.");
        }

        if (publication is { Status: PublicationStatus.Completed, Result: not null })
        {
            return PostContractMapper.MapLiveEvents(publication.Result);
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

        var invite = await store.GetInviteAsync(player.Id, cancellationToken);
        var invited = await store.GetInvitesByInviterAsync(player.Id, cancellationToken);
        var cap = gameDay.GetPostsCap(player, invite, invited);

        if (player.EnergyUsed >= cap)
        {
            throw new RequestConflictException("The daily post limit has been reached.");
        }

        var turn = publication.AssembleTurn();
        StarTurnPolicy.EnsurePostAllowed(turn, world, StarTurnPolicy.IsHateEnabled(player, world, balance.Value, now));

        var wantCompleted = wantId is not null && world.Player.Wants.Any(want =>
            want.Id == wantId && !want.Done);
        if (wantId is not null && !wantCompleted)
        {
            throw new RequestConflictException("The selected daily want is unavailable.");
        }

        var progression = progressionCalculator.Calculate(
            player,
            world,
            turn,
            ownWords,
            wantCompleted,
            publication.Id);
        var updatedWorld = world.PublishPost(
            progression.SubscriberDelta,
            progression.Viral,
            progression.Skills,
            wantId,
            turn.StarId,
            now,
            balance.Value.Relations.Minimum,
            balance.Value.Relations.Maximum,
            balance.Value.Relations.MaxGainPerDay,
            balance.Value.Relations.StarCardAt);
        var updated = player.SpendPost(progression.Skills);
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
        var recentPosts = await sender.Send(new GetChannelPostsQuery(world.Id, player.Id, 20), cancellationToken);
        var recentViews = recentPosts.Select(post => post.Views).ToArray();
        var views = Math.Max(48, updatedWorld.Player.Subscribers);
        var reachRatio = recentViews.Length == 0 ? 1 : views / GetMedian(recentViews);
        var post = new GamePost(
            postId,
            player.Id,
            world.Id,
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
        var updatedInvite = invite is { InviteeBonusUsed: false }
            ? invite.UseInviteeBonus(now).UseInviterBonus(now)
            : null;
        var committed = await store.TryCommitPostAsync(
            updated,
            player.Version,
            updatedWorld,
            world.Version,
            published,
            request.PublicationId,
            updatedInvite,
            cancellationToken);
        if (!committed)
        {
            throw new RequestConflictException("The player state changed while publishing the post.");
        }

        await challengeLifecycle.RecordPostAsync(player.Id, post, cancellationToken);
        TrackProgression(player, updated, world, updatedWorld, post, turn, analytics, invite is not null);
        return PostContractMapper.MapLiveEvents(published);
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
        Reki.Domain.Worlds.World beforeWorld,
        Reki.Domain.Worlds.World afterWorld,
        GamePost post,
        GeneratedPostTurn turn,
        ProductTelemetry analytics,
        bool referred)
    {
        analytics.TrackPlayer(
            after,
            "post_scored",
            new Dictionary<string, string>
            {
                ["world"] = afterWorld.BalanceKey,
                ["brave"] = turn.Brave.ToString("0.###", CultureInfo.InvariantCulture),
                ["humor"] = turn.Humor.ToString("0.###", CultureInfo.InvariantCulture),
                ["topic"] = turn.Topic.ToString("0.###", CultureInfo.InvariantCulture),
                ["quality"] = post.Quality.ToString("0.###", CultureInfo.InvariantCulture),
                ["post_index"] = after.EnergyUsed.ToString(CultureInfo.InvariantCulture),
                ["id"] = post.Id.Value,
                ["delta"] = post.SubscriberDelta.ToString(CultureInfo.InvariantCulture),
            },
            referred);

        foreach ((var skillId, var progress) in after.Skills)
        {
            var previousLevel = before.Skills[skillId].Level;
            if (progress.Level > previousLevel)
            {
                analytics.TrackPlayer(
                    after,
                    "level_up",
                    new Dictionary<string, string>
                    {
                        ["world"] = afterWorld.BalanceKey,
                        ["skill"] = skillId.Value,
                        ["level"] = progress.Level.ToString(CultureInfo.InvariantCulture),
                    },
                    referred);
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
                new Dictionary<string, string> { ["card"] = achievement.Id, ["world"] = afterWorld.BalanceKey },
                referred);
        }
    }
}