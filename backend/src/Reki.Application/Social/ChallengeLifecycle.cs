using System.Globalization;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class ChallengeLifecycle(
    IGameStore store,
    IOptions<GameBalance> balance,
    GameDay gameDay,
    TimeProvider timeProvider,
    ProductTelemetry analytics)
{
    public async ValueTask StartOrAcceptAsync(
        PlayerProfile player,
        PlayerId friendId,
        CancellationToken cancellationToken)
    {
        if (player.Id == friendId)
        {
            throw new RequestConflictException("A player cannot challenge their own channel.");
        }

        PlayerProfile friend = (await store.GetPlayerAsync(friendId, cancellationToken)
                                ?? throw new ResourceNotFoundException("Friend not found.")).EnsureDefaults();
        GameChallenge? pending = await store.GetPendingChallengeAsync(
            player.Id,
            friendId,
            cancellationToken);
        if (pending is not null && pending.InitiatorId == player.Id)
        {
            return;
        }

        if (await store.GetActiveChallengeAsync(player.Id, cancellationToken) is not null ||
            await store.GetActiveChallengeAsync(friendId, cancellationToken) is not null)
        {
            throw new RequestConflictException("One of the players already has an active challenge.");
        }

        if (pending is null)
        {
            GameChallenge invitation = GameChallenge.CreatePending(
                player.Id,
                friendId,
                timeProvider.GetUtcNow(),
                balance.Value.Challenge.Days,
                balance.Value.Challenge.MinimumPosts);
            if (!await store.TryInsertChallengeAsync(invitation, cancellationToken))
            {
                throw new RequestConflictException("The challenge invitation could not be created.");
            }

            return;
        }

        GameChallenge active = pending.Accept(player.Id, gameDay.PeriodStart);
        if (!await store.TryReplaceChallengeAsync(active, pending.Version, cancellationToken))
        {
            throw new RequestConflictException("The challenge state changed while accepting it.");
        }

        await UpdatePlayerAsync(
            player.Id,
            profile => profile.UnlockAchievement(AchievementCatalog.Challenge),
            cancellationToken);
        await UpdatePlayerAsync(
            friend.Id,
            profile => profile.UnlockAchievement(AchievementCatalog.Challenge),
            cancellationToken);
        analytics.Track(
            player.Id,
            "challenge_started",
            new Dictionary<string, string>
            {
                ["days"] = active.Days.ToString(CultureInfo.InvariantCulture),
                ["min_posts"] = active.MinimumPosts.ToString(CultureInfo.InvariantCulture),
            });
    }

    public async ValueTask<ChallengeView?> GetActiveViewAsync(
        PlayerId playerId,
        CancellationToken cancellationToken)
    {
        GameChallenge? challenge = await store.GetActiveChallengeAsync(playerId, cancellationToken);
        if (challenge is null)
        {
            return null;
        }

        DateTimeOffset periodStart = gameDay.PeriodStart;
        if (challenge.IsFinished(periodStart))
        {
            await FinalizeAsync(challenge, periodStart, cancellationToken);
            return null;
        }

        PlayerProfile rival = (await store.GetPlayerAsync(
                challenge.GetRival(playerId),
                cancellationToken) ?? throw new ResourceNotFoundException("Challenge rival not found."))
            .EnsureDefaults();
        return new ChallengeView(playerId, challenge, rival, challenge.GetDay(periodStart));
    }

    public async ValueTask RecordPostAsync(
        PlayerId playerId,
        GamePost post,
        CancellationToken cancellationToken)
    {
        GameChallenge? challenge = await store.GetActiveChallengeAsync(playerId, cancellationToken);
        if (challenge is null)
        {
            return;
        }

        DateTimeOffset periodStart = gameDay.PeriodStart;
        if (challenge.IsFinished(periodStart))
        {
            await FinalizeAsync(challenge, periodStart, cancellationToken);
            return;
        }

        GameChallenge updated = challenge.RecordPost(playerId, post);
        if (await store.TryReplaceChallengeAsync(updated, challenge.Version, cancellationToken))
        {
            return;
        }

        throw new RequestConflictException("The challenge score changed concurrently.");
    }

    private async ValueTask FinalizeAsync(
        GameChallenge challenge,
        DateTimeOffset periodStart,
        CancellationToken cancellationToken)
    {
        GameChallenge completed = challenge.Complete();
        bool replaced = await store.TryReplaceChallengeAsync(
            completed,
            challenge.Version,
            cancellationToken);
        if (!replaced)
        {
            return;
        }

        if (completed.WinnerId is null)
        {
            TrackChallengeFinished(completed);
            return;
        }

        PlayerId winnerId = completed.WinnerId.Value;
        PlayerId loserId = completed.GetRival(winnerId);
        DateTimeOffset bonusUntil = periodStart.AddDays(balance.Value.ChallengeWinBonusDays);
        await UpdatePlayerAsync(
            winnerId,
            profile => profile.ApplyChallengeWinBonus(
                balance.Value.ChallengeWinBonusPerDay,
                bonusUntil),
            cancellationToken);
        await UpdatePlayerAsync(
            loserId,
            profile => profile.UnlockAchievement(AchievementCatalog.Rematch),
            cancellationToken);
        TrackChallengeFinished(completed);
    }

    private void TrackChallengeFinished(GameChallenge challenge)
    {
        analytics.Track(
            challenge.InitiatorId,
            "challenge_finished",
            new Dictionary<string, string>
            {
                ["completed"] = (challenge.WinnerId is not null).ToString(CultureInfo.InvariantCulture),
                ["initiator_posts"] = challenge.InitiatorPerformance.PostCount
                    .ToString(CultureInfo.InvariantCulture),
                ["opponent_posts"] = challenge.OpponentPerformance.PostCount
                    .ToString(CultureInfo.InvariantCulture),
            });
    }

    private async ValueTask UpdatePlayerAsync(
        PlayerId playerId,
        Func<PlayerProfile, PlayerProfile> update,
        CancellationToken cancellationToken)
    {
        PlayerProfile current = (await store.GetPlayerAsync(playerId, cancellationToken)
                                 ?? throw new ResourceNotFoundException("Challenge player not found."))
            .EnsureDefaults();
        PlayerProfile updated = update(current);
        if (ReferenceEquals(current, updated) ||
            await store.TryReplacePlayerAsync(updated, current.Version, cancellationToken))
        {
            return;
        }

        throw new RequestConflictException("The challenge reward could not be applied.");
    }
}