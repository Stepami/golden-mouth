using Reki.Domain.Common;
using Vogen;

namespace Reki.Domain.Social;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct ChallengeId
{
    public static ChallengeId New() => From(Guid.NewGuid().ToString("N"));

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}
