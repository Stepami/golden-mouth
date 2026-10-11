using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Safety;
using Reki.Application.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class SaveWorldRulesHandler(
    IGameStore store,
    ISender sender,
    PlayerSafetyGuard safetyGuard)
    : IRequestHandler<SaveWorldRulesCommand>
{
    public async ValueTask<Unit> Handle(
        SaveWorldRulesCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        await safetyGuard.EnsureInputAllowedAsync(
            player,
            [request.Description, .. request.Rules.Select(rule => rule.Text)],
            cancellationToken);
        var ownWorldId = WorldId.From("own");
        var existingRules = await store.GetWorldRulesAsync(
            player.Id,
            ownWorldId,
            cancellationToken);
        var safeRules = new WorldRules(
            existingRules?.Id ?? WorldRulesId.New(),
            player.Id,
            ownWorldId,
            request.Title,
            request.Presets,
            request.Description,
            [
                .. request.Rules.Where(rule =>
                    !rule.Locked &&
                    !rule.Text.Equals(WorldRulesDefaults.SafetyRule, StringComparison.OrdinalIgnoreCase)),
                new WorldRule(WorldRulesDefaults.SafetyRule, true),
            ]);
        await store.SaveWorldRulesAsync(safeRules, cancellationToken);
        return Unit.Value;
    }
}
