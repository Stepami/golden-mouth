using Reki.Domain.Bonuses;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public interface IBonusRepository
{
    Task<TBonus?> GetActiveBonus<TBonus>(PlayerId id, CancellationToken ct = default)
        where TBonus : Bonus;

    Task SaveBonus(Bonus bonus, CancellationToken ct);
}
