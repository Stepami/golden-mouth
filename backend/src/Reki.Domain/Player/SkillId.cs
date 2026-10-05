using Reki.Domain.Common;
using Vogen;

namespace Reki.Domain.Player;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct SkillId
{
    private static string NormalizeInput(string input) => input.Trim().ToLowerInvariant();

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}
