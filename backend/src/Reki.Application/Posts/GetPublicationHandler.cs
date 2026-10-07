using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Contracts.Posts;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed class GetPublicationHandler(
    IGameStore store,
    PlayerReader playerReader) : IQueryHandler<GetPublicationQuery, PublicationStatusResponse>
{
    public async ValueTask<PublicationStatusResponse> Handle(
        GetPublicationQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        Publication? publication = await store.GetPublicationAsync(request.PublicationId, cancellationToken);
        return publication is not null && publication.PlayerId == player.Id
            ? PostContractMapper.MapPublication(publication)
            : throw new ResourceNotFoundException("Publication not found.");
    }
}
