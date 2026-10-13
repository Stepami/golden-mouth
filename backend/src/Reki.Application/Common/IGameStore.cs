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
    ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken);

    ValueTask<PlayerProfile?> GetPlayerAsync(TelegramUserId telegramUserId, CancellationToken cancellationToken);

    ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask CreateInviteAsync(Invitation invite, CancellationToken cancellationToken);

    ValueTask<Invitation?> GetPendingInviteAsync(PlayerId inviteeId, CancellationToken cancellationToken);

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
        int inviteBonusMaxPerDay,
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

    /// <summary>
    /// Reads posts in a world whose ownership the application has already verified.
    /// </summary>
    ValueTask<IReadOnlyList<GamePost>> GetPostsAsync(
        int? limit,
        WorldId worldId,
        AuthorId? authorId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads a post only in the world whose ownership the application has already verified.
    /// </summary>
    ValueTask<GamePost?> GetPostAsync(
        PostId postId,
        WorldId worldId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads a public story post after verifying the explicitly supplied world's owner.
    /// </summary>
    ValueTask<GamePost?> GetPublicStoryPostAsync(
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