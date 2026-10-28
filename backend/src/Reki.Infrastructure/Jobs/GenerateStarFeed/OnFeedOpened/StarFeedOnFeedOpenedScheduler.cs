using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Posts.AI.Feed;
using Reki.Domain.Posts;
using TickerQ.Utilities.Interfaces.Managers;

namespace Reki.Infrastructure.Jobs.GenerateStarFeed.OnFeedOpened;

internal sealed class StarFeedOnFeedOpenedScheduler(
    ITimeTickerManager<RekiTimeTicker> manager,
    TimeProvider timeProvider,
    IOptionsMonitor<GameBalance> options,
    ILogger<StarFeedOnFeedOpenedScheduler> logger) :
    StarFeedScheduler<GenerateStarFeedOnFeedOpenedJob, StarFeedRequest, ScheduleFromFeedRequest>(
        manager,
        timeProvider,
        logger)
{
    protected override StarFeedRequest CreateRequest(
        ScheduleFromFeedRequest request,
        DateTimeOffset createdAt) =>
        new(
            request.PlayerId,
            request.WorldId,
            PostId.New(createdAt),
            createdAt.ToUnixTimeMilliseconds(),
            Random.Shared.NextDouble() < options.CurrentValue.StarFeedOpenProbability);
}
