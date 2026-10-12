using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Posts;

public sealed class GetPublicationHandler(IGameStore store) : IQueryHandler<GetPublicationQuery, Publication?>
{
    public async ValueTask<Publication?> Handle(GetPublicationQuery request, CancellationToken cancellationToken)
    {
        var publication = await store.GetPublicationAsync(request.PublicationId, cancellationToken);
        return publication is not null &&
               (request.PlayerId is null || publication.PlayerId == request.PlayerId)
            ? publication
            : null;
    }
}
