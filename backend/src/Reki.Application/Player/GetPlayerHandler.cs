using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class GetPlayerHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<GetPlayerQuery, PlayerView>
{
    public async ValueTask<PlayerView> Handle(
        GetPlayerQuery request,
        CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        PlayerProfile normalized = gameDay.Normalize(player);

        if (!ReferenceEquals(player, normalized) && !await store.TryReplacePlayerAsync(
            normalized,
            player.Version,
            cancellationToken))
        {
            normalized = await playerReader.GetAsync(telegramUserId, cancellationToken);
        }

        return playerReader.CreateView(normalized);
    }
}

