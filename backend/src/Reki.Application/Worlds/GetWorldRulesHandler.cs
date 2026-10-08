using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

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
        return request.WorldId.Value == "own" && stored is not null
            ? stored
            : WorldRulesDefaults.Create(player.Id, request.WorldId);
    }
}