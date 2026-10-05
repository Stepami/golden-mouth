using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed class DeletePlayerHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<DeletePlayerCommand>
{
    public async ValueTask<Unit> Handle(
        DeletePlayerCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        await store.DeletePlayerAsync(player, cancellationToken);
        return Unit.Value;
    }
}

