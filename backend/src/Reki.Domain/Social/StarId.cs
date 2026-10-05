using Reki.Domain.Common;
using Vogen;

namespace Reki.Domain.Social;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct StarId
{
    private static string NormalizeInput(string value) => value.Trim().ToLowerInvariant();

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}
