using Vogen;

namespace Reki.Domain.Worlds;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct WorldId
{
    public static WorldId New() => From(Guid.NewGuid().ToString("N"));

    private static string NormalizeInput(string value) => value.Trim().ToLowerInvariant();

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}