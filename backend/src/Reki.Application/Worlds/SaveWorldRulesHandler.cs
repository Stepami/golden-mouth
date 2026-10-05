using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class SaveWorldRulesHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<SaveWorldRulesCommand>
{
    public async ValueTask<Unit> Handle(
        SaveWorldRulesCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
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

