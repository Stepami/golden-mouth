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
        ClaimPlusInterest command,
        CancellationToken cancellationToken)
    {
        var currentBalance = balance.CurrentValue;
        var player = await rekiContextAccessor.Context.Player;

        if (command.Interest)
        {
            var previousVersion = player.Version;
            player.RequestPlusNotification();
            if (player.Version != previousVersion)
            {
                await playerRepository.SavePlusNotificationAsync(player, previousVersion, cancellationToken);
            }
        }

        var claimVersion = player.Version;
        var previousCrystals = player.Wallet.Crystals;
        player.ClaimPlusInterestBonus(currentBalance.PlusInterestBonus);
        if (player.Version == claimVersion)
        {
            return 0;
        }

        return await playerRepository.ClaimPlusInterestAsync(player, claimVersion, previousCrystals, cancellationToken);
    }
}
