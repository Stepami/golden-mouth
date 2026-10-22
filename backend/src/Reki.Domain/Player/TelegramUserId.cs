using Vogen;

namespace Reki.Domain.Player;

[ValueObject<long>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct TelegramUserId
{
    private static Validation Validate(long value) => IdentifierValidation.Positive(value);
}
