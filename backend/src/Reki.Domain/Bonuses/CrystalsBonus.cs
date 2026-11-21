using Reki.Domain.Player;

namespace Reki.Domain.Bonuses;

public sealed class CrystalsBonus(BonusId id, PlayerId playerId, int amount, bool isClaimed = false)
    : Bonus(id, playerId, isClaimed)
{
    public int Amount { get; } = amount;
}
