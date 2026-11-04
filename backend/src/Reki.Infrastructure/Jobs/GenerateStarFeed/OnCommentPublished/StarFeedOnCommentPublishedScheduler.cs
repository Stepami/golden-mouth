using Microsoft.Extensions.Logging;
using Reki.Application.Posts.AI.Comment;
using Reki.Domain.Posts;
using TickerQ.Utilities.Interfaces.Managers;

namespace Reki.Infrastructure.Jobs.GenerateStarFeed.OnCommentPublished;

internal sealed class StarFeedOnCommentPublishedScheduler(
    ITimeTickerManager<RekiTimeTicker> manager,
    TimeProvider timeProvider,
    ILogger<StarFeedOnCommentPublishedScheduler> logger) :
    StarFeedScheduler<GenerateStarFeedOnCommentPublishedJob, StarFeedOnCommentPublishedRequest, ScheduleFromCommentRequest>(
        manager,
        timeProvider,
        logger)
{
    protected override StarFeedOnCommentPublishedRequest CreateRequest(
        ScheduleFromCommentRequest request,
        DateTimeOffset createdAt) =>
        new(
            request.PlayerId,
            request.WorldId,
            PostId.New(createdAt),
            createdAt.ToUnixTimeMilliseconds(),
            request.SourcePostId,
            request.SourceCommentId);
}
