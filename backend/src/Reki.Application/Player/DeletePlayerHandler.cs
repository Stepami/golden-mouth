using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class DeletePlayerHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store, PlayerReader playerReader)
    : IRequestHandler<DeletePlayerCommand>
{
    public async ValueTask<Unit> Handle(
        DeletePlayerCommand request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        await store.DeletePlayerAsync(player, cancellationToken);
        return Unit.Value;
    }
}

