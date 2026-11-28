using Mediator;
using Reki.Application.Player.Responses;

namespace Reki.Application.Player.GetBoosterStock;

internal sealed class BoosterStockQueryHandler(IBonusRepository bonuses) : IQueryHandler<BoosterStockQuery, BoosterStockResponse>
{
    public async ValueTask<BoosterStockResponse> Handle(BoosterStockQuery query, CancellationToken cancellationToken) =>
        await bonuses.GetBoosterStockAsync(query.PlayerId, cancellationToken);
}
