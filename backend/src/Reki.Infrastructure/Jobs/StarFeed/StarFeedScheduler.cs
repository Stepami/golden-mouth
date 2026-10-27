using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs.GenerateStarFeedOnCommentPublished;
using Reki.Infrastructure.Jobs.GenerateStarFeedOnFeedOpened;
using Reki.Infrastructure.Jobs.GenerateStarFeedOnPostPublished;
using TickerQ.Utilities.Interfaces.Managers;

namespace Reki.Infrastructure.Jobs.StarFeed;

internal sealed partial class StarFeedScheduler(
    ITimeTickerManager<RekiTimeTicker> manager,
    TimeProvider timeProvider,
    IOptions<GameBalance> options,
    ILogger<StarFeedScheduler> logger) : IStarFeedScheduler
{
    public async ValueTask ScheduleAsync(
        PlayerId playerId,
        WorldId worldId,
        StarFeedEvent sourceEvent,
        PostId? sourcePostId,
        CommentId? sourceCommentId)
    {
        var now = timeProvider.GetUtcNow();
        var postId = PostId.New(now);
        var result = sourceEvent.Value switch
        {
            StarFeedEventType.FeedOpened when sourcePostId is null && sourceCommentId is null =>
                await manager.AddAsync<GenerateStarFeedOnFeedOpenedJob, StarFeedRequest>(
                    now.UtcDateTime, new StarFeedRequest(playerId, worldId, postId, now.ToUnixTimeMilliseconds())),
            StarFeedEventType.PostPublished when sourcePostId is { } sourceId && sourceCommentId is null =>
                await manager.AddAsync<GenerateStarFeedOnPostPublishedJob, StarFeedOnPostPublishedRequest>(
                    now.UtcDateTime, new StarFeedOnPostPublishedRequest(playerId, worldId, postId, now.ToUnixTimeMilliseconds(), sourceId)),
            StarFeedEventType.CommentPublished when sourcePostId is { } sourceId && sourceCommentId is { } commentId =>
                await manager.AddAsync<GenerateStarFeedOnCommentPublishedJob, StarFeedOnCommentPublishedRequest>(
                    now.UtcDateTime, new StarFeedOnCommentPublishedRequest(playerId, worldId, postId, now.ToUnixTimeMilliseconds(), sourceId, commentId)),
            _ => throw new InvalidOperationException("Invalid feed source."),
        };
        if (!result.IsSucceeded)
        {
            SchedulingFailed();
        }
    }

    public ValueTask ScheduleFromFeedAsync(PlayerId playerId, WorldId worldId) =>
        Random.Shared.NextDouble() < options.Value.StarFeedOpenProbability
            ? ScheduleAsync(playerId, worldId, StarFeedEvent.From(StarFeedEventType.FeedOpened), null, null)
            : ValueTask.CompletedTask;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Star feed scheduling did not succeed")]
    private partial void SchedulingFailed();
}
