using Mediator;

namespace Reki.Application.Posts.GetPublication;

public sealed class PublicationQueryHandler(IPublicationRepository publicationRepository) : IQueryHandler<PublicationQuery, Publication?>
{
    public async ValueTask<Publication?> Handle(PublicationQuery request, CancellationToken cancellationToken)
    {
        var publication = await publicationRepository.GetPublicationAsync(request.PublicationId, cancellationToken);
        return publication is not null &&
               (request.PlayerId is null || publication.PlayerId == request.PlayerId)
            ? publication
            : null;
    }
}
