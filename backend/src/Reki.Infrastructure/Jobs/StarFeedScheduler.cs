using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Reki.Application.Common;
using Reki.Application.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using TickerQ.Utilities.Interfaces.Managers;

namespace Reki.Infrastructure.Jobs;

public sealed partial class StarFeedScheduler(
    ITimeTickerManager<RekiTimeTicker> manager,
    TimeProvider timeProvider,
    IOptions<GameBalance> options,
    ILogger<StarFeedScheduler> logger) : IStarFeedScheduler
{
    public async ValueTask ScheduleAsync(
        PlayerId playerId,
        WorldId worldId,
        string trigger,
        string triggerId,
        CancellationToken cancellationToken)
    {
        if (trigger == "feed" && Random.Shared.NextDouble() >= options.Value.StarFeedOpenProbability)
        {
            return;
        }

        try
        {
            var result = await manager.AddAsync<GenerateStarFeedPostJob, StarFeedRequest>(
                timeProvider.GetUtcNow().UtcDateTime,
                new StarFeedRequest(playerId, worldId, PostId.From(ObjectId.GenerateNewId().ToString()), trigger, triggerId),
                cancellationToken);
            if (!result.IsSucceeded)
            {
                SchedulingFailed();
            }
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException))
        {
            // Background activity must not change an already committed HTTP business result.
            SchedulingFailed();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Star feed scheduling did not succeed.")]
    private partial void SchedulingFailed();
}