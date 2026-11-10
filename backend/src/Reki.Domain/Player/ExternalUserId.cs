using Vogen;

namespace Reki.Domain.Player;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct ExternalUserId
{
    public static ExternalUserId Telegram(long id) => From($"telegram:{id.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

    public static ExternalUserId Vk(string id) => From($"vk:{id}");

    public static ExternalUserId Yandex(string id) => From($"yandex:{id}");

    private static Validation Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.IndexOf(':') > 0 && !value.EndsWith(':')
            ? Validation.Ok
            : Validation.Invalid("External provider and identifier are required.");
}
