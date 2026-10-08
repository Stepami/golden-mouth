using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed class ClaimPlusInterestHandler(
    IGameStore store,
    GameBalance balance,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<ClaimPlusInterestCommand, int>
{
    public async ValueTask<int> Handle(
        ClaimPlusInterestCommand request,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            PlayerProfile storedPlayer = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
            PlayerProfile player = gameDay.Normalize(storedPlayer);
            int bonus = player.PlusInterestClaimed ? 0 : balance.PlusInterestBonus;
            PlayerProfile updated = player.ClaimPlusInterestBonus(balance.PlusInterestBonus) with
            {
                PlusNotifyRequested = player.PlusNotifyRequested || request.Interest,
            };
            if (updated == storedPlayer)
            {
                return 0;
            }

            updated = updated with { Version = storedPlayer.Version + 1 };
            if (await store.TryReplacePlayerAsync(updated, storedPlayer.Version, cancellationToken))
            {
                return bonus;
            }
        }

        throw new RequestConflictException("The player changed while claiming the bonus. Retry the request.");
    }
}

