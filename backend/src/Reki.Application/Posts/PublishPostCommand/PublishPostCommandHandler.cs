using System.Globalization;
using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Invites;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.GetPublication;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Application.Posts.Responses.LiveEvent;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.PublishPostCommand;

internal sealed class PublishPostCommandHandler(
    IInvitationRepository invitationRepository,
    IPublicationRepository publicationRepository,
    IGameStore store,
    IOptionsMonitor<GameBalance> balance,
    PostProgressionCalculator progressionCalculator,
    TimeProvider timeProvider,
    ISender sender,
    IRekiContextAccessor rekiContextAccessor,
    ProductTelemetry analytics,
    PlayerSafetyGuard safety,
    StarTurnPolicy turnPolicy,
    IStarFeedScheduler feedScheduler) : ICommandHandler<PublishPost, IReadOnlyList<LiveEvent>>
{
    public async ValueTask<IReadOnlyList<LiveEvent>> Handle(
        PublishPost request,
        CancellationToken cancellationToken)
    {
        var publication = await sender.Send(new PublicationQuery(request.PublicationId), cancellationToken);
        var storedPlayer = await rekiContextAccessor.Context.Player;
        var worlds = await rekiContextAccessor.Context.Worlds;
        var world = worlds.GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        if (publication is null || publication.PlayerId != storedPlayer.Id || publication.WorldId != world.Id)
        {
            throw new ResourceNotFoundException("Publication not found.");
        }

        if (publication is { Status: PublicationStatusType.Completed, Result: not null })
        {
            return publication.Result.MapLiveEvents(world, storedPlayer.Id);
        }

        if (publication.Status != PublicationStatusType.ReadyForModeration)
        {
            throw new RequestConflictException("The publication is not ready for moderation.");
        }

        var currentBalance = balance.CurrentValue;
        var text = publication.Text;
        var ownWords = publication.OwnWords;
        var wantId = publication.WantId;
        var now = timeProvider.GetUtcNow();
        var player = storedPlayer;

        if (player.Energy < currentBalance.PostEnergyCost)
        {
            throw new RequestConflictException("The daily post limit has been reached.");
        }

        var turn = publication.AssembleTurn();
        try
        {
            await safety.EnsureInputAllowedAsync(player, [publication.Text], cancellationToken);
            PlayerSafetyGuard.EnsureOutputAllowed([turn.Comment, turn.Hook]);
        }
        catch (Exception exception) when (exception is ContentRejectedException or UnsafeGeneratedContentException)
        {
            var category = exception is ContentRejectedException rejected ? rejected.Category : ((UnsafeGeneratedContentException)exception).Category;
            var failure = new PublicationFailure(exception is ContentRejectedException ? "content_rejected" : "unsafe_generated_content", category, PublicationSafetyPolicy.Version);
            await publicationRepository.MarkPublicationFailedAsync(publication.Id, now.AddDays(1).UtcDateTime, failure, cancellationToken);
            throw;
        }

        turnPolicy.EnsurePostAllowed(turn, player, world);

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
            currentBalance.Relations.Minimum,
            currentBalance.Relations.Maximum,
            currentBalance.Relations.MaxGainPerDay,
            currentBalance.Relations.StarCardAt);
        var updated = player.SpendPost(progression.Skills, currentBalance.PostEnergyCost) with
        {
            HitOwnWorldMilestone = player.HitOwnWorldMilestone || updatedWorld.Player.Subscribers >= currentBalance.OwnWorldSubscribers,
        };
        var postId = PostId.New(now);
        var starComment = new GameComment(
            CommentId.New(),
            null,
            null,
            turn.StarId.ToAuthorId(),
            turn.Comment,
            false,
            []);
        var recentPosts = await sender.Send(new WorldPostsQuery(world.Id, Cursor: null, PageSize: 20), cancellationToken);
        var recentViews = recentPosts.Items.Select(post => post.Views).ToArray();
        var views = Math.Max(48, updatedWorld.Player.Subscribers);
        var reachRatio = recentViews.Length == 0 ? 1 : views / GetMedian(recentViews);
        var post = new GamePost(
            postId,
            player.Id.ToAuthorId(),
            world.Id,
            text,
            views,
            [starComment],
            now,
            progression.SubscriberDelta,
            progression.Quality,
            reachRatio,
            progression.Viral,
            progression.Flop);
        var hook = turn.Hook;

        var published = new PublishedPost(post, hook, turn.StarName, turn.Reaction, turn.Reasons);
        var invite = await invitationRepository.GetPendingInviteAsync(player.Id, cancellationToken);
        var updatedInvite = invite?.UseInviteeBonus(now).UseInviterBonus(now);
        updated = updated with { Energy = updated.Energy + (updatedInvite?.BonusAmount ?? 0) };

        var committed = await store.TryCommitPostAsync(
            updated,
            player.Version,
            updatedWorld,
            world.Version,
            published,
            request.PublicationId,
            updatedInvite,
            currentBalance.InviteBonusMaxPerDay,
            cancellationToken);
        if (!committed)
        {
            throw new RequestConflictException("The player state changed while publishing the post.");
        }

        analytics.TrackWorldAudience(updatedWorld);
        TrackProgression(player, updated, world, updatedWorld, post, turn, analytics);
        await feedScheduler.ScheduleFromPostAsync(player.Id, world.Id, post.Id);
        return published.MapLiveEvents(updatedWorld, player.Id);
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
        World beforeWorld,
        World afterWorld,
        GamePost post,
        GeneratedPostTurn turn,
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
