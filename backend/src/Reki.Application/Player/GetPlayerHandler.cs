using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class GetPlayerHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    GameDay gameDay) : IRequestHandler<GetPlayerQuery, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(GetPlayerQuery request, CancellationToken cancellationToken)
    {
        var player = await ReadAsync(request, cancellationToken);
        if (!request.NormalizeDay)
        {
            return player;
        }

        var normalized = gameDay.Normalize(player);
        if (!ReferenceEquals(player, normalized) && !await store.TryReplacePlayerAsync(
            normalized,
            player.Version,
            cancellationToken))
        {
            normalized = await ReadAsync(request, cancellationToken);
        }

        return normalized;
    }

    private async ValueTask<PlayerProfile> ReadAsync(GetPlayerQuery request, CancellationToken cancellationToken)
    {
        var player = request.PlayerId is { } playerId
            ? await store.GetPlayerAsync(playerId, cancellationToken)
            : await store.GetPlayerAsync(TelegramUserId.From(telegramUser.User.Id), cancellationToken);
        return (player ?? throw new ResourceNotFoundException("Player not found.")).EnsureDefaults();
    }
}
