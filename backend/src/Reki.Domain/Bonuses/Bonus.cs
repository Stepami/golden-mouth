using Reki.Domain.Player;

namespace Reki.Domain.Bonuses;

public abstract class Bonus(BonusId id, PlayerId playerId, bool isClaimed)
{
    public BonusId Id { get; } = id;

    public PlayerId PlayerId { get; } = playerId;

    public bool IsClaimed { get; private set; } = isClaimed;

    public void Claim() => IsClaimed = true;
}
