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
        return await ReadAsync(request, cancellationToken);
    }

    private async ValueTask<PlayerProfile> ReadAsync(GetPlayerQuery request, CancellationToken cancellationToken)
    {
        var player = request.PlayerId is { } playerId
            ? await store.GetPlayerAsync(playerId, cancellationToken)
            : await store.GetPlayerAsync(TelegramUserId.From(telegramUser.User.Id), cancellationToken);
        return player ?? throw new ResourceNotFoundException("Player not found.");
    }
}