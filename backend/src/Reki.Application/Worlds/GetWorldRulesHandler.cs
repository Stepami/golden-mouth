using Mediator;
using Reki.Application.Common;
using Reki.Application.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class GetWorldRulesHandler(
    IGameStore store,
    ISender sender)
    : IRequestHandler<GetWorldRulesQuery, WorldRules>
{
    public async ValueTask<WorldRules> Handle(
        GetWorldRulesQuery request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var stored = await store.GetWorldRulesAsync(
            player.Id,
            request.WorldId,
            cancellationToken);
        return request.WorldId.Value == "own" && stored is not null
            ? stored
            : WorldRulesDefaults.Create(player.Id, request.WorldId);
    }
}