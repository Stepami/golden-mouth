using Reki.Application.Posts;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application;

/// <summary>Coordinates persistence operations involving multiple entities.</summary>
public interface IGameStore
{
    ValueTask<bool> TryCreatePostAsync(GamePost post, PlayerProfile player, int expectedPlayerVersion, CancellationToken cancellationToken);

    ValueTask<bool> TryApplyPostAsync(GamePost post, int expectedPostVersion, PlayerProfile player, int expectedPlayerVersion, World world, int expectedWorldVersion, Invitation? updatedInvite, int inviteBonusMaxPerDay, CancellationToken cancellationToken);

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

    ValueTask<bool> TryCommitReplyAsync(
        GamePost updatedPost,
        int expectedPostVersion,
        string requestId,
        PlayerProfile updatedPlayer,
        int expectedPlayerVersion,
        World updatedWorld,
        int expectedWorldVersion,
        CancellationToken cancellationToken);
}
