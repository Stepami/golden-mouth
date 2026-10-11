using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class ClaimPlusInterestHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    IOptions<GameBalance> balance,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<ClaimPlusInterestCommand, int>
{
    public async ValueTask<int> Handle(
        ClaimPlusInterestCommand request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var storedPlayer = await playerReader.GetAsync(telegramUserId, cancellationToken);
        var player = gameDay.Normalize(storedPlayer);
        if (!ReferenceEquals(player, storedPlayer))
        {
            await store.StartPlayerPeriodAsync(storedPlayer, player, cancellationToken);
        }

        if (request.Interest)
        {
            var subscribed = player.RequestPlusNotification();
            if (!ReferenceEquals(subscribed, player))
            {
                await store.SavePlusNotificationAsync(player, subscribed, cancellationToken);
                player = subscribed;
            }
        }

        var updated = player.ClaimPlusInterestBonus(balance.Value.PlusInterestBonus);
        if (ReferenceEquals(updated, player))
        {
            return 0;
        }

        return await store.TryClaimPlusInterestAsync(player, updated, cancellationToken)
            ? updated.DailyBonus - player.DailyBonus
            : 0;
    }
}