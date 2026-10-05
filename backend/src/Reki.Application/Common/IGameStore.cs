using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;

namespace Reki.Application.Common;

public interface IGameStore
{
    ValueTask<PlayerProfile?> GetPlayerAsync(TelegramUserId telegramUserId, CancellationToken cancellationToken);

    ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask UpsertPlayerAsync(PlayerProfile player, CancellationToken cancellationToken);

    ValueTask<bool> TryReplacePlayerAsync(
        PlayerProfile player,
        int expectedVersion,
        CancellationToken cancellationToken);

    ValueTask DeletePlayerAsync(PlayerProfile player, CancellationToken cancellationToken);

    ValueTask AddPostAsync(GamePost post, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<GamePost>> GetRecentPostsAsync(
        PlayerId playerId,
        int count,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<long>> GetRecentPostViewsAsync(
        PlayerId playerId,
        int count,
        CancellationToken cancellationToken);

    ValueTask<GamePost?> GetPostAsync(
        PlayerId playerId,
        PostId postId,
        CancellationToken cancellationToken);

    ValueTask<bool> AddReplyAsync(
        PlayerId playerId,
        PostId postId,
        GameComment reply,
        CancellationToken cancellationToken);

    ValueTask<StarConversation?> GetStarConversationAsync(
        PlayerId playerId,
        StarId starId,
        CancellationToken cancellationToken);

    ValueTask SaveStarConversationAsync(
        StarConversation conversation,
        CancellationToken cancellationToken);

    ValueTask<DailyGameEvent?> GetTodayEventAsync(
        PlayerId playerId,
        DateTimeOffset dayStartedAtUtc,
        CancellationToken cancellationToken);

    ValueTask SaveEventAsync(DailyGameEvent gameEvent, CancellationToken cancellationToken);

    ValueTask<GameChallenge?> GetActiveChallengeAsync(
        PlayerId playerId,
        CancellationToken cancellationToken);

    ValueTask<GameChallenge?> GetPendingChallengeAsync(
        PlayerId firstPlayerId,
        PlayerId secondPlayerId,
        CancellationToken cancellationToken);

    ValueTask<bool> TryInsertChallengeAsync(
        GameChallenge challenge,
        CancellationToken cancellationToken);

    ValueTask<bool> TryReplaceChallengeAsync(
        GameChallenge challenge,
        int expectedVersion,
        CancellationToken cancellationToken);

    ValueTask<WorldRules?> GetWorldRulesAsync(
        PlayerId playerId,
        WorldId worldId,
        CancellationToken cancellationToken);

    ValueTask SaveWorldRulesAsync(WorldRules rules, CancellationToken cancellationToken);

    ValueTask AppendThreadAsync(
        PlayerId playerId,
        PostId postId,
        string hook,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken);
}
