using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using TickerQ.Utilities.Interfaces.Managers;

namespace Reki.Infrastructure.Jobs.GenerateStarFeedPost;

internal sealed partial class StarFeedScheduler(
    ITimeTickerManager<RekiTimeTicker> manager,
    TimeProvider timeProvider,
    IOptions<GameBalance> options,
    ILogger<StarFeedScheduler> logger) : IStarFeedScheduler
{
    public async ValueTask ScheduleAsync(PlayerId playerId, WorldId worldId)
    {
        var now = timeProvider.GetUtcNow();
        var result = await manager.AddAsync<GenerateStarFeedPostJob, StarFeedRequest>(
            now.UtcDateTime,
            new StarFeedRequest(playerId, worldId, PostId.New(now)));
        if (!result.IsSucceeded)
        {
            SchedulingFailed();
        }
    }

    public ValueTask ScheduleFromFeedAsync(PlayerId playerId, WorldId worldId) =>
        Random.Shared.NextDouble() < options.Value.StarFeedOpenProbability
            ? ScheduleAsync(playerId, worldId)
            : ValueTask.CompletedTask;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Star feed scheduling did not succeed")]
    private partial void SchedulingFailed();
}