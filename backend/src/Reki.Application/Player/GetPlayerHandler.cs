using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class GetPlayerHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store) : IRequestHandler<GetPlayerQuery, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(GetPlayerQuery request, CancellationToken cancellationToken)
    {
        return await ReadAsync(cancellationToken);
    }

    private async ValueTask<PlayerProfile> ReadAsync(CancellationToken cancellationToken)
    {
        var player = await store.GetPlayerAsync(TelegramUserId.From(telegramUser.User.Id), cancellationToken);
        return player ?? throw new ResourceNotFoundException("Player not found.");
    }
}