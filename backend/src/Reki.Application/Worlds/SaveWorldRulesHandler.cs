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
        WorldRules safeRules = request.Rules with
        {
            PlayerId = player.Id,
            WorldId = "own",
            Rules =
            [
                new WorldRule("Контент подходит для 14+", true),
                .. request.Rules.Rules.Where(rule => !rule.Locked),
            ],
        };
        await store.SaveWorldRulesAsync(safeRules, cancellationToken);
        return Unit.Value;
    }
}