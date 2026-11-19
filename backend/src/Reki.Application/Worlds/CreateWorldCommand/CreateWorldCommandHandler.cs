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
    IStarVectorStore starVectorStore,
    TimeProvider timeProvider,
    IProductGoalRepository productGoals) : ICommandHandler<CreateWorld, ProductGoalResult<World>>
{
    public async ValueTask<ProductGoalResult<World>> Handle(CreateWorld command, CancellationToken cancellationToken)
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
        await productGoals.EnsurePlayerAsync(player.Id, cancellationToken);
        var goals = await unitOfWork.ExecuteAsync(
            async token =>
            {
                var previousVersion = player.Version;
                player.AddDailyChallengeWorld(world, currentBalance.DailyChallengeTargets[player.DailyChallenge.Day.DayOfWeek]);
                await worldRepository.InsertWorldAsync(world, token);
                if (player.Version != previousVersion)
                {
                    await players.SaveDailyChallengeAsync(player, previousVersion, token);
                }

                return await productGoals.RecordActivityAsync(player.Id, timeProvider.GetUtcNow(), ordinaryPost: false, token);
            },
            _ => worldCache.UpdateWorld(world),
            cancellationToken);
        telemetry.TrackPlayer(player, "world_created", new Dictionary<string, string> { ["template_id"] = world.TemplateId?.ToString() ?? string.Empty, ["difficulty"] = world.Difficulty.ToString().ToLowerInvariant() }, world.Id);
        telemetry.TrackWorldAudience(world);
        await starVectorStore.WarmUpAsync(world);
        return new ProductGoalResult<World>(world, goals);
    }
}
