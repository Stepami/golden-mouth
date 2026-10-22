using Reki.Application.Player;
using Reki.Application.Player.ClaimPlusInterestCommand;
using Reki.Application.Player.OnboardPlayerCommand;
using Reki.Application.Player.UpdateSettingsCommand;
using Reki.Domain.Player;
using Riok.Mapperly.Abstractions;

namespace Reki.Api.Requests.Player;

[Mapper]
internal static partial class PlayerRequestMapper
{
    public static OnboardPlayer ToCommand(this OnboardingRequest source, TelegramUserId telegramUserId) =>
        new(telegramUserId, source.BirthGroup.ToStorageValue(), source.Address.ToString().ToLowerInvariant(), source.ReferralCode, source.AgeBand);

    [MapProperty(nameof(UpdateSettingsRequest.Address), nameof(UpdateSettings.Address), Use = nameof(MapAddress))]
    public static partial UpdateSettings ToCommand(this UpdateSettingsRequest source);

    public static ClaimPlusInterest ToCommand(this PlusInterestRequest? source) => new(source?.Interest ?? true);

    private static string? MapAddress(Address? source) => source?.ToString().ToLowerInvariant();

    private static string ToStorageValue(this BirthGroup source) => source switch
    {
        BirthGroup.Teen => "14-17",
        BirthGroup.Adult => "18+",
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };
}
