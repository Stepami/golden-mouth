using Vogen;

namespace Reki.Domain.Player;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct ExternalUserId
{
    public static ExternalUserId Telegram(long id) => From($"telegram:{id.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

    private static Validation Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.IndexOf(':') > 0 && !value.EndsWith(':')
            ? Validation.Ok
            : Validation.Invalid("External provider and identifier are required.");
}
