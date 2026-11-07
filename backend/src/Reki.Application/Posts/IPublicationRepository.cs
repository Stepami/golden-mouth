using Reki.Domain.Player;

namespace Reki.Application.Posts;

public interface IPublicationRepository
{
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
        PublicationFailure failure,
        CancellationToken cancellationToken);

    ValueTask<bool> TryCompletePublicationAsync(
        string id,
        PlayerId playerId,
        PublishedPost published,
        CancellationToken cancellationToken);

    ValueTask DeletePublicationsAsync(PlayerId playerId, CancellationToken cancellationToken);
}