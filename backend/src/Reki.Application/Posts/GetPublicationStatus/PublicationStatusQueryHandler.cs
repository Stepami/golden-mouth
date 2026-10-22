using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.GetPublication;
using Reki.Application.RekiContext;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts.GetPublicationStatus;

internal sealed class PublicationStatusQueryHandler(
    ISender sender,
    IRekiContextAccessor rekiContextAccessor)
    : IQueryHandler<PublicationStatusQuery, PublicationStatusResponse>
{
    public async ValueTask<PublicationStatusResponse> Handle(
        PublicationStatusQuery request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        var publication =
            await sender.Send(new PublicationQuery(request.PublicationId, player.Id), cancellationToken)
            ?? throw new ResourceNotFoundException("Publication not found.");
        var worlds = await rekiContextAccessor.Context.Worlds;
        var world = worlds.GetValueOrDefault(publication.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        return PostContractMapper.MapPublication(publication, world);
    }
}