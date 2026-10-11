using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class UpdateWorldHandler(
    IGameStore store,
    ISender sender,
    PlayerSafetyGuard safetyGuard)
    : IRequestHandler<UpdateWorldCommand>
{
    public async ValueTask<Unit> Handle(
        UpdateWorldCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        if (request.Description is not null || request.Rules is not null)
        {
            await safetyGuard.EnsureInputAllowedAsync(
                player,
                [request.Description ?? string.Empty, .. (request.Rules ?? []).Select(rule => rule.Text)],
                cancellationToken);
        }

        var world = await sender.Send(new GetWorldQuery(player.Id, request.WorldId), cancellationToken);
        var updated = world with
        {
            Version = world.Version + 1,
            Rules = request.Rules is null ? world.Rules :
            [
                .. request.Rules.Where(rule => !rule.Locked && rule.Text != "Без тем 18+ и травли по внешности"),
                new WorldRule("Без тем 18+ и травли по внешности", true),
            ],
            Interface = world.Interface with
            {
                Title = request.Title ?? world.Interface.Title,
                Description = request.Description ?? world.Interface.Description,
            },
            Player = world.Player with
            {
                Drama = request.Drama ?? world.Player.Drama,
                Voice = request.Voice ?? world.Player.Voice,
            },
        };
        if (!await store.TryReplaceWorldAsync(updated, world.Version, cancellationToken))
        {
            throw new RequestConflictException("World changed concurrently.");
        }

        return Unit.Value;
    }
}