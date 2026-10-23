using Vogen;

namespace Reki.Domain.Events;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct RekiEventId
{
    public static RekiEventId New() => From(Guid.NewGuid());

    public static RekiEventId From(string value) => From(Guid.Parse(value));

    public override string ToString() => ToString("N", System.Globalization.CultureInfo.InvariantCulture);
}