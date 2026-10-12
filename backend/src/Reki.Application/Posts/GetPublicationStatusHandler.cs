using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Application.Player;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed class GetPublicationStatusHandler(ISender sender)
    : IRequestHandler<GetPublicationStatusQuery, PublicationStatusResponse>
{
    public async ValueTask<PublicationStatusResponse> Handle(
        GetPublicationStatusQuery request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var publication =
            await sender.Send(new GetPublicationQuery(request.PublicationId, player.Id), cancellationToken)
            ?? throw new ResourceNotFoundException("Publication not found.");
        var world = await sender.Send(new Worlds.GetWorldQuery(player.Id, publication.WorldId), cancellationToken);
        return PostContractMapper.MapPublication(publication, world);
    }
}