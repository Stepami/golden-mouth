using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.RekiContext;

namespace Reki.Application.Player.ClaimPlusInterestCommand;

internal sealed class ClaimPlusInterestCommandHandler(
    IPlayerRepository playerRepository,
    IOptionsMonitor<GameBalance> balance,
    IRekiContextAccessor rekiContextAccessor) : ICommandHandler<ClaimPlusInterest, int>
{
    public async ValueTask<int> Handle(
        ClaimPlusInterest request,
        CancellationToken cancellationToken)
    {
        var currentBalance = balance.CurrentValue;
        var storedPlayer = await rekiContextAccessor.Context.Player;
        var player = storedPlayer;

        if (request.Interest)
        {
            var subscribed = player.RequestPlusNotification();
            if (!ReferenceEquals(subscribed, player))
            {
                await playerRepository.SavePlusNotificationAsync(player, subscribed, cancellationToken);
                player = subscribed;
            }
        }

        var updated = player.ClaimPlusInterestBonus(currentBalance.PlusInterestBonus);
        if (ReferenceEquals(updated, player))
        {
            return 0;
        }

        return await playerRepository.TryClaimPlusInterestAsync(player, updated, cancellationToken)
            ? updated.Wallet.Energy - player.Wallet.Energy
            : 0;
    }
}
