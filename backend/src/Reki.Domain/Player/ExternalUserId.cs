using Vogen;

namespace Reki.Domain.Player;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct ExternalUserId
{
    private static Validation Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.IndexOf(':') > 0 && !value.EndsWith(':')
            ? Validation.Ok
            : Validation.Invalid("External provider and identifier are required.");
}
