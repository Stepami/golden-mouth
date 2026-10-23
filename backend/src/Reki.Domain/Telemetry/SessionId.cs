using Vogen;

namespace Reki.Domain.Telemetry;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct SessionId
{
    public static SessionId New() => From(Guid.NewGuid());

    public static SessionId From(string value) => From(Guid.Parse(value));

    public override string ToString() => ToString("N", System.Globalization.CultureInfo.InvariantCulture);
}
