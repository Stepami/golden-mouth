using Reki.Domain.Common;
using Vogen;

namespace Reki.Domain.Events;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct RekiEventId
{
    public static RekiEventId New() => From(Guid.NewGuid().ToString("N"));

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}
