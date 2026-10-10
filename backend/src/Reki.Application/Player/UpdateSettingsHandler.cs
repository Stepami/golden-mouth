using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class UpdateSettingsHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store)
    : IRequestHandler<UpdateSettingsCommand>
{
    public async ValueTask<Unit> Handle(
        UpdateSettingsCommand request,
        CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        if (await store.UpdatePlayerSettingsAsync(telegramUserId, request, cancellationToken))
        {
            return Unit.Value;
        }

        throw new ResourceNotFoundException("Player not found.");
    }
}