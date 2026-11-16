using Vogen;

namespace Reki.Domain.Player.Streaks;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct StreakId
{
    public static StreakId New(DateTimeOffset startedAt) => From(Guid.CreateVersion7(startedAt));

    public static StreakId From(string value) => From(Guid.Parse(value));

    public override string ToString() => ToString("N", System.Globalization.CultureInfo.InvariantCulture);
}
