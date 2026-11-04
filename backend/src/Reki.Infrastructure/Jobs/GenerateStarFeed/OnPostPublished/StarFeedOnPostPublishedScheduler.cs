using Microsoft.Extensions.Logging;
using Reki.Application.Posts.AI.Post;
using Reki.Domain.Posts;
using TickerQ.Utilities.Interfaces.Managers;

namespace Reki.Infrastructure.Jobs.GenerateStarFeed.OnPostPublished;

internal sealed class StarFeedOnPostPublishedScheduler(
    ITimeTickerManager<RekiTimeTicker> manager,
    TimeProvider timeProvider,
    ILogger<StarFeedOnPostPublishedScheduler> logger) :
    StarFeedScheduler<GenerateStarFeedOnPostPublishedJob, StarFeedOnPostPublishedRequest, ScheduleFromPostRequest>(
        manager,
        timeProvider,
        logger)
{
    protected override StarFeedOnPostPublishedRequest CreateRequest(
        ScheduleFromPostRequest request,
        DateTimeOffset createdAt) =>
        new(
            request.PlayerId,
            request.WorldId,
            PostId.New(createdAt),
            createdAt.ToUnixTimeMilliseconds(),
            request.SourcePostId);
}
