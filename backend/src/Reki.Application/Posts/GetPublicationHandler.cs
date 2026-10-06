using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed class GetPublicationHandler(
    IGameStore store,
    PlayerReader playerReader) : IQueryHandler<GetPublicationQuery, Publication>
{
    public async ValueTask<Publication> Handle(
        GetPublicationQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        Publication? publication = await store.GetPublicationAsync(request.PublicationId, cancellationToken);
        return publication is not null && publication.PlayerId == player.Id
            ? publication
            : throw new ResourceNotFoundException("Publication not found.");
    }
}
