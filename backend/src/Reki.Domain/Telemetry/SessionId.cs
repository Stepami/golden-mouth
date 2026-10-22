using Vogen;

namespace Reki.Domain.Telemetry;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct SessionId
{
    public static SessionId New() => From(Guid.NewGuid().ToString("N"));

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}
