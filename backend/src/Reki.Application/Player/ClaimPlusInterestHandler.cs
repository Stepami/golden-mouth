using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;

namespace Reki.Application.Player;

public sealed class ClaimPlusInterestHandler(
    IGameStore store,
    IOptions<GameBalance> balance,
    GameDay gameDay,
    ISender sender) : IRequestHandler<ClaimPlusInterestCommand, int>
{
    public async ValueTask<int> Handle(
        ClaimPlusInterestCommand request,
        CancellationToken cancellationToken)
    {
        var storedPlayer = await sender.Send(new GetPlayerQuery(), cancellationToken);
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