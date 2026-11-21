using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Player;
using Reki.Application.Posts.ML;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.CreateWorldCommand;

internal sealed class CreateWorldCommandHandler(
    IWorldRepository worldRepository,
    IWorldPlayerCache worldCache,
    IPlayerRepository players,
    IUnitOfWork unitOfWork,
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
        var world = source.Instantiate(player.Id, currentBalance.Growth.StartingSubscribers);
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                var current = await players.GetPlayerAsync(player.Id, token)
                              ?? throw new ResourceNotFoundException("Player not found.");
                var previousVersion = current.Version;
                current.AddDailyChallengeWorld(
                    world,
                    currentBalance.DailyChallengeTargets[current.DailyChallenge.Day.DayOfWeek]);
                await worldRepository.InsertWorldAsync(world, token);
                if (current.Version != previousVersion)
                {
                    await players.SaveDailyChallengeAsync(current, previousVersion, token);
                }

                return true;
            },
            _ => worldCache.UpdateWorld(world),
            cancellationToken);
        telemetry.TrackPlayer(
            player,
            "world_created",
            new Dictionary<string, string>
            {
                ["template_id"] = world.TemplateId?.ToString() ?? string.Empty,
                ["difficulty"] = world.Difficulty.ToString().ToLowerInvariant(),
            },
            world.Id);
        telemetry.TrackWorldAudience(world);
        await starVectorStore.WarmUpAsync(world);
        return world;
    }
}
