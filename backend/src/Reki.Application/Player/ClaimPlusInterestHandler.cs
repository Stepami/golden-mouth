using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;

namespace Reki.Application.Player;

public sealed class ClaimPlusInterestHandler(
    IGameStore store,
    IOptions<GameBalance> balance,
    IRekiContextAccessor contextAccessor) : IRequestHandler<ClaimPlusInterestCommand, int>
{
    public async ValueTask<int> Handle(
        ClaimPlusInterestCommand request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var storedPlayer = await rekiContext.Player;
        var player = storedPlayer;

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
            ? updated.Energy - player.Energy
            : 0;
    }
}