using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Player;
using Reki.Contracts.Player;
using Reki.Domain.Invites;
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
        var invited = await store.GetInvitesByInviterAsync(player.Id, cancellationToken);
        var postsCap = GetPostsCap(player, invite, invited);
        var firstPostInviteBonus = invite is { InviteeBonusUsed: false } ? balance.Value.InviteBonusBoth : 0;
        return new PlayerResponse(
            player.Id.Value,
            Enum.Parse<Address>(player.Address, true),
            Math.Max(0, postsCap - player.EnergyUsed),
            postsCap,
            firstPostInviteBonus,
            new StreakResponse(player.StreakDays, Enumerable.Range(0, 7).Select(day => day < Math.Min(7, player.StreakDays)).ToArray(), "ещё день — и бонус"),
            player.Skills.ToDictionary(item => Enum.Parse<ContractSkillId>(item.Key.Value, true), item => MapSkill(item.Value)),
            new PlayerSettingsResponse(player.Settings.Notifications, player.Settings.Quiet, player.Settings.Plus),
            new Dictionary<string, string>());
    }

    public int GetPostsCap(PlayerProfile player, Invitation? invite, IReadOnlyList<Invitation> invited)
    {
        var start = player.PostsPeriodStartedAtUtc;
        var end = start.AddDays(1);
        var inviterUses = invited.Count(item => item.InviterBonusUsed &&
            item.InviterBonusUsedAtUtc >= start && item.InviterBonusUsedAtUtc < end);
        var inviteeUse = invite is { InviteeBonusUsed: true } &&
            invite.InviteeBonusUsedAtUtc >= start && invite.InviteeBonusUsedAtUtc < end ? 1 : 0;
        return player.GetPostsCap(balance.Value.FreePostsPerDay, balance.Value.PlusPostsPerDay) +
            (balance.Value.InviteBonusBoth * (Math.Min(balance.Value.InviteBonusMaxPerDay, inviterUses) + inviteeUse));
    }

    [MapperIgnoreSource(nameof(SkillProgress.Experience))]
    private partial SkillResponse MapSkill(SkillProgress source);
}
