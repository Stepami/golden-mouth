using Reki.Application.Player.Responses;
using Reki.Domain.Player;
using Riok.Mapperly.Abstractions;

namespace Reki.Application.Player;

[Mapper]
public static partial class PlayerResponseMapper
{
    public static PlayerResponse ToPlayerResponse(this PlayerProfile player)
    {
        var streakDays = player.Streaks.Count > 0 && player.Streaks[^1].IsActive ? player.Streaks[^1].Count : 0;
        return new PlayerResponse(
            player.Id,
            player.Address,
            player.Wallet.Energy,
            new StreakResponse(streakDays, Enumerable.Range(0, 7).Select(day => day < Math.Min(7, streakDays)).ToArray(), "ещё день — и бонус"),
            MapSkills(player.Skills),
            MapSettings(player.Settings),
            new Dictionary<string, string>(),
            player.BirthYear,
            player.HitOwnWorldMilestone,
            player.ToEnergyWalletResponse());
    }

    public static EnergyWalletResponse ToResponse(this PlayerWallet wallet) => new(
        wallet.Energy,
        PlayerWallet.Cap,
        wallet.NextEnergyAtUtc,
        PlayerWallet.RegenSeconds,
        wallet.Crystals,
        new EnergyExchangeQuoteResponse(
            PlayerWallet.ExchangeCost,
            PlayerWallet.Cap,
            wallet.ExchangeAvailable,
            wallet.ExchangeUnavailableReason));

    public static EnergyWalletResponse ToEnergyWalletResponse(this PlayerProfile player) => player.Wallet.ToResponse();

    private static partial IReadOnlyDictionary<SkillId, SkillResponse> MapSkills(IReadOnlyDictionary<SkillId, SkillProgress> source);

    private static partial PlayerSettingsResponse MapSettings(PlayerSettings source);

    [MapperIgnoreSource(nameof(SkillProgress.Experience))]
    private static partial SkillResponse MapSkill(SkillProgress source);
}
