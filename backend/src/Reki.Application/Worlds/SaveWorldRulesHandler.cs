using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Safety;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class SaveWorldRulesHandler(
    IGameStore store,
    PlayerReader playerReader,
    PlayerSafetyGuard safetyGuard)
    : IRequestHandler<SaveWorldRulesCommand>
{
    public async ValueTask<Unit> Handle(
        SaveWorldRulesCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        await safetyGuard.EnsureInputAllowedAsync(
            player,
            [request.Description, .. request.Rules.Select(rule => rule.Text)],
            cancellationToken);
        WorldId ownWorldId = WorldId.From("own");
        WorldRules? existingRules = await store.GetWorldRulesAsync(
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
