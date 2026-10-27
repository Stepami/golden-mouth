using Vogen;

namespace Reki.Domain.Worlds;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct WorldId
{
    public static WorldId New() => From(Guid.NewGuid());

    public static WorldId From(string value) => From(Guid.Parse(value));

    public override string ToString() => ToString("D", System.Globalization.CultureInfo.InvariantCulture);
}
