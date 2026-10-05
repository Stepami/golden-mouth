using Mediator;
using Reki.Application.Game.Common;
using Reki.Domain.Game.Player;
using Reki.Domain.Game.Worlds;

namespace Reki.Application.Game.Worlds;

public sealed class GetWorldRulesHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetWorldRulesQuery, WorldRules>
{
    public async ValueTask<WorldRules> Handle(
        GetWorldRulesQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        WorldRules? stored = await store.GetWorldRulesAsync(
            player.Id,
            request.WorldId,
            cancellationToken);
        return stored ?? CreateDefaults(player.Id, request.WorldId);
    }

    private static WorldRules CreateDefaults(string playerId, string worldId)
    {
        string title = worldId switch
        {
            "academy" => "Академия",
            "it" => "Айти-тусовка",
            "stream" => "Стрим",
            "own" => "Свой мир",
            _ => "Сцена",
        };
        return new WorldRules(
            playerId,
            worldId,
            title,
            ["дерзко", "по-доброму", "загадочно"],
            "Правила мира определяют, за что растёт канал.",
            [
                new WorldRule("Контент подходит для 14+", true),
                new WorldRule("Пиши в голосе своего канала", false),
            ]);
    }
}

