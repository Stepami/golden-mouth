using Reki.Application.Posts;
using Reki.Application.Posts.PublishPostCommand;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application;

/// <summary>Coordinates persistence operations involving multiple entities.</summary>
public interface IGameStore
{
    ValueTask DeletePlayerAsync(PlayerProfile player, CancellationToken cancellationToken);

    ValueTask<PostCommitResult> TryCommitPostAsync(
        PlayerProfile updatedPlayer,
        int expectedPlayerVersion,
        World updatedWorld,
        int expectedWorldVersion,
        PublishedPost published,
        string publicationId,
        Invitation? updatedInvite,
        PlayerProfile? updatedInviter,
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
