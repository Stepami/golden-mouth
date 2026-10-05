using Reki.Domain.Game;

namespace Reki.Application.Game;

public interface IGameStore
{
    ValueTask<PlayerProfile?> GetPlayerAsync(long telegramUserId, CancellationToken cancellationToken);

    ValueTask UpsertPlayerAsync(PlayerProfile player, CancellationToken cancellationToken);

    ValueTask<bool> TryReplacePlayerAsync(
        PlayerProfile player,
        int expectedVersion,
        CancellationToken cancellationToken);

    ValueTask DeletePlayerAsync(PlayerProfile player, CancellationToken cancellationToken);

    ValueTask AddPostAsync(GamePost post, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<GamePost>> GetRecentPostsAsync(
        string playerId,
        int count,
        CancellationToken cancellationToken);

    ValueTask<bool> AddReplyAsync(
        string playerId,
        string postId,
        GameComment reply,
        CancellationToken cancellationToken);

    ValueTask<StarConversation?> GetStarConversationAsync(
        string playerId,
        string starId,
        CancellationToken cancellationToken);

    ValueTask SaveStarConversationAsync(
        StarConversation conversation,
        CancellationToken cancellationToken);

    ValueTask<DailyGameEvent?> GetTodayEventAsync(
        string playerId,
        DateTimeOffset dayStartedAtUtc,
        CancellationToken cancellationToken);

    ValueTask SaveEventAsync(DailyGameEvent gameEvent, CancellationToken cancellationToken);

    ValueTask<GameChallenge?> GetActiveChallengeAsync(
        string playerId,
        CancellationToken cancellationToken);

    ValueTask SaveChallengeAsync(GameChallenge challenge, CancellationToken cancellationToken);

    ValueTask<WorldRules?> GetWorldRulesAsync(
        string playerId,
        string worldId,
        CancellationToken cancellationToken);

    ValueTask SaveWorldRulesAsync(WorldRules rules, CancellationToken cancellationToken);

    ValueTask AppendAnalyticsAsync(AnalyticsBatch batch, CancellationToken cancellationToken);

    ValueTask AppendThreadAsync(
        string playerId,
        string postId,
        string hook,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken);
}
