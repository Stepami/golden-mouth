using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

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
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        player = gameDay.Normalize(player);
        PlayerProfile updated = player.ClaimPlusInterestBonus(balance.PlusInterestBonus);
        await store.UpsertPlayerAsync(updated, cancellationToken);
        return updated == player ? 0 : balance.PlusInterestBonus;
    }
}

