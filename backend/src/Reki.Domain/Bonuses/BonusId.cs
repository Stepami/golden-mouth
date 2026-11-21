using Vogen;

namespace Reki.Domain.Bonuses;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct BonusId
{
    public static BonusId New() => From(Guid.CreateVersion7());
}
