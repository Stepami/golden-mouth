using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.CreateWorldCommand;

public sealed class CreateWorldCommandHandler(
    IWorldRepository worldRepository,
    IRekiContextAccessor rekiContextAccessor,
    GameDayPeriod gameDayPeriod,
    IOptions<GameBalance> balance,
    ProductTelemetry telemetry)
    : ICommandHandler<CreateWorld, World>
{
    public async ValueTask<World> Handle(CreateWorld request, CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        var source = request.Definition;
        if (source.BalanceKey == "own" && !player.HitOwnWorldMilestone)
        {
            throw new RequestConflictException("Creating a world from scratch is not unlocked.");
        }

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
        await worldRepository.InsertWorldAsync(world, cancellationToken);
        telemetry.TrackPlayer(player, "world_created", new Dictionary<string, string> { ["world"] = world.BalanceKey }, world.Id);
        telemetry.TrackWorldAudience(world);
        return world;
    }
}
