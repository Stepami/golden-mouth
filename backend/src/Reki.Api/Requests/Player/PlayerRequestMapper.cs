using Reki.Application.Player.ClaimPlusInterestCommand;
using Reki.Application.Player.OnboardPlayerCommand;
using Reki.Application.Player.UpdateSettingsCommand;
using Reki.Domain.Player;
using Riok.Mapperly.Abstractions;

namespace Reki.Api.Requests.Player;

[Mapper]
internal static partial class PlayerRequestMapper
{
    public static OnboardPlayer ToOnboardPlayer(this OnboardingRequest source, PlayerId playerId) =>
        new(playerId, source.BirthGroup.ToStorageValue(), source.Address, source.ReferralCode, source.AgeBand);

    public static partial UpdateSettings ToUpdateSettings(this UpdateSettingsRequest source);

    public static ClaimPlusInterest ToClaimPlusInterest(this PlusInterestRequest? source) => new(source?.Interest ?? true);

    private static string ToStorageValue(this BirthGroup source) => source switch
    {
        BirthGroup.Teen => "14-17",
        BirthGroup.Adult => "18+",
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };
}
