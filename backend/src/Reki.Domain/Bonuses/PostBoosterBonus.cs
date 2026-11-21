using Reki.Domain.Player;

namespace Reki.Domain.Bonuses;

public sealed class PostBoosterBonus(BonusId id, PlayerId playerId, DateTimeOffset activeUntil, bool isClaimed = false)
    : Bonus(id, playerId, isClaimed)
{
    public DateTimeOffset ActiveUntil { get; } = activeUntil;

    public void Claim(DateTimeOffset now)
    {
        if (now >= ActiveUntil)
        {
            Claim();
        }
    }
}
