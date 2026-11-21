using Reki.Domain.Player;

namespace Reki.Domain.Bonuses;

public sealed class ViralCardBonus(BonusId id, PlayerId playerId, int multiplier, bool isClaimed = false)
    : Bonus(id, playerId, isClaimed)
{
    public int Multiplier { get; } = multiplier;
}
