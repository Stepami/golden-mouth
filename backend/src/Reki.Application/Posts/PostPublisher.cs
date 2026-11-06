using System.Globalization;
using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Invites;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

internal sealed class PostPublisher(
    IGameStore store,
    IInvitationRepository invitations,
    IOptionsMonitor<GameBalance> balance,
    PostProgressionCalculator calculator,
    TimeProvider timeProvider,
    ISender sender,
    ProductTelemetry telemetry,
    IStarFeedScheduler<ScheduleFromPostRequest> feedScheduler)
{
    public async ValueTask<PublishedPost?> PublishAsync(
        GamePost post,
        PlayerProfile player,
        World world,
        GeneratedPostTurn turn,
        string? publicationId,
        CancellationToken cancellationToken)
    {
        var legacy = publicationId is not null;
        var now = timeProvider.GetUtcNow();
        var current = balance.CurrentValue;
        var wantCompleted = post.WantId is not null && world.Player.Wants.Any(want => want.Id == post.WantId && !want.Done);
        var scoringPlayer = legacy ? player : player with { EnergyUsed = post.EnergyIndex - 1 };
        var progression = calculator.Calculate(scoringPlayer, world, turn, post.OwnWords, wantCompleted, publicationId ?? post.RequestId ?? post.Id.ToString());
        var updatedWorld = world.PublishPost(
            progression.SubscriberDelta,
            progression.Viral,
            progression.Skills,
            wantCompleted ? post.WantId : null,
            turn.StarId,
            now,
            current.Relations.Minimum,
            current.Relations.Maximum,
            current.Relations.MaxGainPerDay,
            current.Relations.StarCardAt);
        var updatedPlayer = legacy
            ? player.SpendPost(progression.Skills, current.PostEnergyCost)
            : player with { Skills = progression.Skills, Version = player.Version + 1 };
        updatedPlayer = updatedPlayer with
        {
            HitOwnWorldMilestone = player.HitOwnWorldMilestone || updatedWorld.Player.Subscribers >= current.OwnWorldSubscribers,
        };
        var recent = await sender.Send(new WorldPostsQuery(world.Id, null, legacy ? 20 : 3), cancellationToken);
        var previousViews = recent.Items.Where(item => item.Id != post.Id).Select(item => item.Views).Order().ToArray();
        var views = Math.Max(48, updatedWorld.Player.Subscribers);
        var median = previousViews.Length == 0 ? views : GetMedian(previousViews);
        var updatedPost = post with
        {
            EvaluationApplied = true,
            PredictedAuthorId = turn.StarId.ToAuthorId(),
            Emoji = turn.Reaction,
            Hook = turn.Hook,
            Reasons = turn.Reasons.Select(reason => new PostReason(reason.Icon, reason.Label)).ToArray(),
            SubscriberDelta = progression.SubscriberDelta,
            Quality = progression.Quality,
            ReachRatio = views / median,
            Viral = progression.Viral,
            Flop = progression.Flop,
            Views = views,
            Version = legacy ? post.Version : post.Version + 1,
        };
        var published = new PublishedPost(updatedPost, turn.Hook, turn.StarName, turn.Reaction, turn.Reasons);
        var invite = await invitations.GetPendingInviteAsync(player.Id, cancellationToken);
        var updatedInvite = invite?.UseInviteeBonus(now).UseInviterBonus(now);
        updatedPlayer = updatedPlayer with { Energy = updatedPlayer.Energy + (updatedInvite?.BonusAmount ?? 0) };
        bool committed;
        if (publicationId is not null)
        {
            committed = await store.TryCommitPostAsync(updatedPlayer, player.Version, updatedWorld, world.Version, published, publicationId, updatedInvite, current.InviteBonusMaxPerDay, cancellationToken);
        }
        else
        {
            committed = await store.TryApplyPostAsync(updatedPost, post.Version, updatedPlayer, player.Version, updatedWorld, world.Version, updatedInvite, current.InviteBonusMaxPerDay, cancellationToken);
        }

        if (!committed)
        {
            return null;
        }

        telemetry.TrackWorldAudience(updatedWorld);
        TrackProgression(player, updatedPlayer, world, updatedWorld, updatedPost, turn, telemetry);
        await feedScheduler.ScheduleFeedAsync(new ScheduleFromPostRequest(player.Id, world.Id, post.Id));
        return published;
    }

    private static double GetMedian(long[] values) =>
        values.Length % 2 == 0
            ? Math.Max(1, (values[(values.Length / 2) - 1] + values[values.Length / 2]) / 2.0)
            : Math.Max(1, values[values.Length / 2]);

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
