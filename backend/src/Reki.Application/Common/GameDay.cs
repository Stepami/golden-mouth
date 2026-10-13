using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Player;
using Reki.Contracts.Player;
using Reki.Domain.Player;
using Riok.Mapperly.Abstractions;
using ContractSkillId = Reki.Contracts.Player.SkillId;

namespace Reki.Application.Common;

[Mapper]
public sealed partial class GameDay(IOptions<GameBalance> balance, IGameStore store, ISender sender)
{
    public async ValueTask<PlayerResponse> GetPlayerAsync(CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var invite = await store.GetInviteAsync(player.Id, cancellationToken);
        var firstPostInviteBonus = invite is { InviteeBonusUsed: false } ? balance.Value.InviteBonusBoth : 0;
        return new PlayerResponse(
            player.Id.Value,
            Enum.Parse<Address>(player.Address, true),
            player.Energy,
            firstPostInviteBonus,
            new StreakResponse(player.StreakDays, Enumerable.Range(0, 7).Select(day => day < Math.Min(7, player.StreakDays)).ToArray(), "ещё день — и бонус"),
            player.Skills.ToDictionary(item => Enum.Parse<ContractSkillId>(item.Key.Value, true), item => MapSkill(item.Value)),
            new PlayerSettingsResponse(player.Settings.Notifications, player.Settings.Quiet, player.Settings.Plus),
            new Dictionary<string, string>());
    }

    [MapperIgnoreSource(nameof(SkillProgress.Experience))]
    private partial SkillResponse MapSkill(SkillProgress source);
}
