using Vogen;

namespace Reki.Domain.Player;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
[Instance("Brave", "brave")]
[Instance("Humor", "humor")]
[Instance("Topic", "topic")]
public readonly partial struct SkillId
{
    private static string NormalizeInput(string input) => input.Trim().ToLowerInvariant();

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}
