using Reki.Contracts.Player;
using Reki.Domain.Player;
using Riok.Mapperly.Abstractions;
using ContractSkillId = Reki.Contracts.Player.SkillId;

namespace Reki.Application.Player;

[Mapper]
public static partial class PlayerResponseMapper
{
    public static PlayerResponse ToResponse(this PlayerProfile player)
    {
        return new PlayerResponse(
            player.Id.Value,
            Enum.Parse<Address>(player.Address, true),
            player.Energy,
            new StreakResponse(player.StreakDays, Enumerable.Range(0, 7).Select(day => day < Math.Min(7, player.StreakDays)).ToArray(), "ещё день — и бонус"),
            player.Skills.ToDictionary(item => Enum.Parse<ContractSkillId>(item.Key.Value, true), item => MapSkill(item.Value)),
            new PlayerSettingsResponse(player.Settings.Notifications, player.Settings.Quiet, player.Settings.Plus),
            new Dictionary<string, string>());
    }

    [MapperIgnoreSource(nameof(SkillProgress.Experience))]
    private static partial SkillResponse MapSkill(SkillProgress source);
}
