using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed class GetPublicationStatusHandler(ISender sender, IRekiContextAccessor contextAccessor)
    : IRequestHandler<GetPublicationStatusQuery, PublicationStatusResponse>
{
    public async ValueTask<PublicationStatusResponse> Handle(
        GetPublicationStatusQuery request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        var publication =
            await sender.Send(new GetPublicationQuery(request.PublicationId, player.Id), cancellationToken)
            ?? throw new ResourceNotFoundException("Publication not found.");
        var world = (await rekiContext.Worlds).GetValueOrDefault(publication.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        return PostContractMapper.MapPublication(publication, world);
    }
}