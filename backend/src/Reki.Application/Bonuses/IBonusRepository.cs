using Reki.Domain.Bonuses;
using Reki.Domain.Player;

namespace Reki.Application.Bonuses;

public interface IBonusRepository
{
#pragma warning disable AV1554 // The bonus repository contract allows callers to omit cancellation.
    Task<TBonus?> GetActiveBonus<TBonus>(PlayerId id, CancellationToken ct = default)
        where TBonus : Bonus;
#pragma warning restore AV1554

    Task SaveBonus(Bonus bonus, CancellationToken ct);

    ValueTask DeleteForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);
}
