using Reki.Application.Player.Responses;
using Reki.Domain.Bonuses;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public interface IBonusRepository
{
    Task<BoosterStockResponse> GetBoosterStockAsync(PlayerId id, CancellationToken ct);

    Task<TBonus?> TryClaimBonus<TBonus>(PlayerId id, CancellationToken ct)
        where TBonus : Bonus;

    Task InsertBonusesAsync(IReadOnlyList<Bonus> bonuses, CancellationToken ct);

    Task DeleteForPlayerAsync(PlayerId id, CancellationToken ct);
}
