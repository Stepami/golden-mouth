using Reki.Application.Player.Responses;
using Reki.Domain.Player;
using Riok.Mapperly.Abstractions;

namespace Reki.Application.Player;

[Mapper]
public static partial class PlayerResponseMapper
{
    public static PlayerResponse ToResponse(this PlayerProfile player)
    {
        return new PlayerResponse(
            player.Id,
            player.Address,
            player.Energy,
            new StreakResponse(player.StreakDays, Enumerable.Range(0, 7).Select(day => day < Math.Min(7, player.StreakDays)).ToArray(), "ещё день — и бонус"),
            MapSkills(player.Skills),
            MapSettings(player.Settings),
            new Dictionary<string, string>(),
            player.HitOwnWorldMilestone,
            player.AgeBand ?? (player.BirthGroup == "14-17" ? "14-17" : "18+"),
            player.ToEnergyWalletResponse());
    }

    public static EnergyWalletResponse ToEnergyWalletResponse(this PlayerProfile player)
    {
        var crystals = player.Crystals ?? throw new InvalidOperationException("Wallet has not been initialized.");
        var reason = crystals < EnergyWallet.ExchangeCost ? "insufficient_crystals" : null;
        if (player.Energy >= EnergyWallet.Cap)
        {
            reason = "energy_full";
        }

        return new EnergyWalletResponse(
            player.Energy,
            EnergyWallet.Cap,
            player.Energy < EnergyWallet.Cap ? player.EnergyUpdatedAtUtc?.AddSeconds(EnergyWallet.RegenSeconds) : null,
            EnergyWallet.RegenSeconds,
            crystals,
            player.Version,
            new EnergyExchangeQuoteResponse(EnergyWallet.ExchangeCost, EnergyWallet.Cap, reason is null, reason));
    }

    private static partial IReadOnlyDictionary<SkillId, SkillResponse> MapSkills(IReadOnlyDictionary<SkillId, SkillProgress> source);

    private static partial PlayerSettingsResponse MapSettings(PlayerSettings source);

    [MapperIgnoreSource(nameof(SkillProgress.Experience))]
    private static partial SkillResponse MapSkill(SkillProgress source);
}
