using Microsoft.Extensions.Logging;
using Reki.Application.Posts;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Interfaces.Managers;

namespace Reki.Infrastructure.Jobs.GenerateStarFeed;

internal abstract partial class StarFeedScheduler<TJob, TRequest, TRekiRequest>(
    ITimeTickerManager<RekiTimeTicker> manager,
    TimeProvider timeProvider,
    ILogger logger) : IStarFeedScheduler<TRekiRequest>
    where TJob : class, ITickerFunction<TRequest>
{
    public async ValueTask ScheduleFeedAsync(TRekiRequest request)
    {
        var now = timeProvider.GetUtcNow();
        var jobRequest = CreateRequest(request, now);
        var result = await manager.AddAsync<TJob, TRequest>(now.UtcDateTime, jobRequest);
        if (!result.IsSucceeded)
        {
            SchedulingFailed();
        }
    }

    protected abstract TRequest CreateRequest(TRekiRequest request, DateTimeOffset createdAt);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Star feed scheduling did not succeed")]
    private partial void SchedulingFailed();
}
