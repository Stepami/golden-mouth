using Reki.Application.Player.Responses;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Player.Streaks;
using Riok.Mapperly.Abstractions;

namespace Reki.Application.Player;

[Mapper]
public static partial class PlayerResponseMapper
{
    public static PlayerResponse ToPlayerResponse(this PlayerProfile player)
    {
        return new PlayerResponse(
            player.Id,
            player.Address,
            player.Wallet.Energy,
            MapSkills(player.Skills),
            MapSettings(player.Settings),
            player.BirthYear,
            MapDailyChallenge(player.DailyChallenge),
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

    [MapperIgnoreSource(nameof(PlayerStreak.PlayerId))]
    public static partial PlayerStreakResponse ToPlayerStreakResponse(this PlayerStreak streak);

    private static partial IReadOnlyDictionary<SkillId, SkillResponse> MapSkills(IReadOnlyDictionary<SkillId, SkillProgress> source);

    private static partial PlayerSettingsResponse MapSettings(PlayerSettings source);

    [MapperIgnoreSource(nameof(PlayerChallenge.IsCompleted))]
    private static partial DailyChallengeResponse MapDailyChallenge(PlayerChallenge source);

    [MapperIgnoreSource(nameof(DailyTaskValues.BaseTarget))]
    private static partial DailyTaskValuesResponse MapDailyTaskValues(DailyTaskValues source);

    [MapperIgnoreSource(nameof(SkillProgress.Experience))]
    private static partial SkillResponse MapSkill(SkillProgress source);
}
