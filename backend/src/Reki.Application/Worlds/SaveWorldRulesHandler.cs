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
            [request.Rules.Description, .. request.Rules.Rules.Select(rule => rule.Text)],
            cancellationToken);
        WorldRules? existingRules = await store.GetWorldRulesAsync(
            player.Id,
            "own",
            cancellationToken);
        WorldRules safeRules = request.Rules with
        {
            Id = existingRules?.Id ?? request.Rules.Id,
            PlayerId = player.Id,
            WorldId = "own",
            Rules =
            [
                .. request.Rules.Rules.Where(rule =>
                    !rule.Locked &&
                    !rule.Text.Equals(WorldRulesDefaults.SafetyRule, StringComparison.OrdinalIgnoreCase)),
                new WorldRule(WorldRulesDefaults.SafetyRule, true),
            ],
        };
        await store.SaveWorldRulesAsync(safeRules, cancellationToken);
        return Unit.Value;
    }
}
