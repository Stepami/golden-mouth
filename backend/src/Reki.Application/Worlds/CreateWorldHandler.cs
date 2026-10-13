using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Telemetry;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class CreateWorldHandler(IGameStore store, IRekiContextAccessor contextAccessor, GameDayPeriod gameDayPeriod, PlayerSafetyGuard safetyGuard, IOptions<GameBalance> balance, ProductTelemetry telemetry)
    : IRequestHandler<CreateWorldCommand, World>
{
    public async ValueTask<World> Handle(CreateWorldCommand request, CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        var source = request.Definition;
        if (source.Stars.Count == 0 || source.Stars.Any(star => !Enum.IsDefined(star.Trait)) ||
            source.Stars.Select(star => star.Id).Distinct().Count() != source.Stars.Count)
        {
            throw new ContentRejectedException();
        }

        var instance = source.Instantiate(player.Id, gameDayPeriod.PeriodStart);
        var world = instance with
        {
            Player = instance.Player with { Subscribers = balance.Value.Growth.StartingSubscribers },
        };
        await safetyGuard.EnsureInputAllowedAsync(
            player,
            [world.Interface.Title, world.Interface.Description, world.Player.Name, .. world.Rules.Select(rule => rule.Text), .. world.Stars.Select(star => star.About)],
            cancellationToken);
        await store.InsertWorldAsync(world, cancellationToken);
        telemetry.TrackPlayer(player, "world_created", new Dictionary<string, string> { ["world"] = world.BalanceKey }, world.Id);
        telemetry.TrackWorldAudience(world);
        return world;
    }
}