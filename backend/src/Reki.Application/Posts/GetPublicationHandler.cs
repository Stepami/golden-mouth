using Mediator;

namespace Reki.Application.Posts;

public sealed class GetPublicationHandler(IPublicationRepository publicationRepository) : IQueryHandler<GetPublicationQuery, Publication?>
{
    public async ValueTask<Publication?> Handle(GetPublicationQuery request, CancellationToken cancellationToken)
    {
        var publication = await publicationRepository.GetPublicationAsync(request.PublicationId, cancellationToken);
        return publication is not null &&
               (request.PlayerId is null || publication.PlayerId == request.PlayerId)
            ? publication
            : null;
    }
}
