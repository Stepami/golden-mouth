using Reki.Application.Posts;
using Reki.Domain.Events;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Common;

public interface IGameStore
{
    ValueTask<long> CountPlayersAsync(CancellationToken cancellationToken);

    ValueTask<PlayerProfile?> GetPlayerAsync(TelegramUserId telegramUserId, CancellationToken cancellationToken);

    ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask CreateInviteAsync(Invitation invite, CancellationToken cancellationToken);

    ValueTask<Invitation?> GetInviteAsync(PlayerId inviteeId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<Invitation>> GetInvitesByInviterAsync(
        PlayerId inviterId,
        CancellationToken cancellationToken);

    ValueTask CreatePlayerAsync(
        PlayerProfile player,
        CancellationToken cancellationToken);

    ValueTask InsertWorldAsync(World world, CancellationToken cancellationToken);

    ValueTask SavePlusNotificationAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken);

    ValueTask<bool> TryClaimPlusInterestAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken);

    ValueTask<bool> TryReplacePlayerAsync(
        PlayerProfile player,
        int expectedVersion,
        CancellationToken cancellationToken);

    ValueTask DeletePlayerAsync(PlayerProfile player, CancellationToken cancellationToken);

    ValueTask<bool> TryCommitPostAsync(
        PlayerProfile updatedPlayer,
        int expectedPlayerVersion,
        World updatedWorld,
        int expectedWorldVersion,
        PublishedPost published,
        string publicationId,
        Invitation? updatedInvite,
        CancellationToken cancellationToken);

    ValueTask<Publication?> GetPublicationAsync(string id, CancellationToken cancellationToken);

    ValueTask<bool> TryCreatePublicationAsync(Publication publication, CancellationToken cancellationToken);

    ValueTask AppendPublicationPartAsync(
        string id,
        PublicationPart part,
        CancellationToken cancellationToken);

    ValueTask MarkPublicationReadyAsync(string id, CancellationToken cancellationToken);

    ValueTask MarkPublicationFailedAsync(
        string id,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<GamePost>> GetPostsAsync(
        PlayerId playerId,
        int? limit,
        CancellationToken cancellationToken,
        WorldId worldId);

    ValueTask<GamePost?> GetPostAsync(
        PostId postId,
        PlayerId playerId,
        CancellationToken cancellationToken);

    ValueTask<bool> TryCommitReplyAsync(
        GamePost updatedPost,
        int expectedPostVersion,
        string requestId,
        PlayerProfile updatedPlayer,
        int expectedPlayerVersion,
        World updatedWorld,
        int expectedWorldVersion,
        CancellationToken cancellationToken);

    ValueTask<StarConversation?> GetStarConversationAsync(
        PlayerId playerId,
        StarId starId,
        CancellationToken cancellationToken,
        WorldId worldId);

    ValueTask SaveStarConversationAsync(
        StarConversation conversation,
        CancellationToken cancellationToken);

    ValueTask<DailyGameEvent?> GetTodayEventAsync(
        PlayerId playerId,
        DateTimeOffset dayStartedAtUtc,
        CancellationToken cancellationToken,
        WorldId worldId);

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

    ValueTask<World?> GetWorldAsync(
        PlayerId playerId,
        WorldId worldId,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<World>> GetWorldTemplatesAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask<bool> TryReplaceWorldAsync(World world, int expectedVersion, CancellationToken cancellationToken);
}