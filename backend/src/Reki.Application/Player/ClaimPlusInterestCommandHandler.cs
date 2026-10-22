using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;

namespace Reki.Application.Player;

public sealed class ClaimPlusInterestCommandHandler(
    IPlayerRepository playerRepository,
    IOptions<GameBalance> balance,
    IRekiContextAccessor rekiContextAccessor) : ICommandHandler<ClaimPlusInterest, int>
{
    public async ValueTask<int> Handle(
        ClaimPlusInterest request,
        CancellationToken cancellationToken)
    {
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

        var updated = player.ClaimPlusInterestBonus(balance.Value.PlusInterestBonus);
        if (ReferenceEquals(updated, player))
        {
            return 0;
        }

        return await playerRepository.TryClaimPlusInterestAsync(player, updated, cancellationToken)
            ? updated.Energy - player.Energy
            : 0;
    }
}
