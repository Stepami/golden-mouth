using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Player;

public sealed class DeletePlayerHandler(
    IGameStore store,
    ISender sender)
    : IRequestHandler<DeletePlayerCommand>
{
    public async ValueTask<Unit> Handle(
        DeletePlayerCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        await store.DeletePlayerAsync(player, cancellationToken);
        return Unit.Value;
    }
}

