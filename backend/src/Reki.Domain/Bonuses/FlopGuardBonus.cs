using Reki.Domain.Player;

namespace Reki.Domain.Bonuses;

public sealed class FlopGuardBonus(BonusId id, PlayerId playerId, int adder, bool isClaimed = false)
    : Bonus(id, playerId, isClaimed)
{
    public int Adder { get; } = adder;
}
