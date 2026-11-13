using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Posts.ML;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.CreateWorldCommand;

internal sealed class CreateWorldCommandHandler(
    IWorldRepository worldRepository,
    IRekiContextAccessor rekiContextAccessor,
    IOptionsMonitor<GameBalance> balance,
    ProductTelemetry telemetry,
    IStarVectorStore starVectorStore) : ICommandHandler<CreateWorld, World>
{
    public async ValueTask<World> Handle(CreateWorld command, CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        var source = command.Definition;
        if (source.TemplateId is null && !player.HitOwnWorldMilestone)
        {
            throw new RequestConflictException("Creating a world from scratch is not unlocked.");
        }

        if (source.Stars.Count == 0 || source.Stars.Any(star => !Enum.IsDefined(star.Trait)) ||
            source.Stars.Select(star => star.Id).Distinct().Count() != source.Stars.Count)
        {
            throw new ContentRejectedException();
        }

        var currentBalance = balance.CurrentValue;
        var instance = source.Instantiate(player.Id);
        var world = instance with
        {
            Player = instance.Player with { Subscribers = currentBalance.Growth.StartingSubscribers },
        };
        await worldRepository.InsertWorldAsync(world, cancellationToken);
        telemetry.TrackPlayer(player, "world_created", new Dictionary<string, string> { ["template_id"] = world.TemplateId?.ToString() ?? string.Empty, ["difficulty"] = world.Difficulty.ToString().ToLowerInvariant() }, world.Id);
        telemetry.TrackWorldAudience(world);
        await starVectorStore.WarmUpAsync(world);
        return world;
    }
}
