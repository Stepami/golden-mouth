using Reki.Domain.Common;
using Vogen;

namespace Reki.Domain.Worlds;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct WorldRulesId
{
    public static WorldRulesId New() => From(Guid.NewGuid().ToString("N"));

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}
