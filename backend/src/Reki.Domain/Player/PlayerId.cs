using Reki.Domain.Common;
using Vogen;

namespace Reki.Domain.Player;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct PlayerId
{
    public static PlayerId New() => From(Guid.NewGuid().ToString("N"));

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}
