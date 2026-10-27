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
    public async ValueTask ScheduleFromPostAsync(PlayerId playerId, WorldId worldId, PostId sourcePostId)
    {
        var now = timeProvider.GetUtcNow();
        var result = await manager.AddAsync<GenerateStarFeedOnPostPublishedJob, StarFeedOnPostPublishedRequest>(
            now.UtcDateTime,
            new StarFeedOnPostPublishedRequest(playerId, worldId, PostId.New(now), now.ToUnixTimeMilliseconds(), sourcePostId));
        if (!result.IsSucceeded)
        {
            SchedulingFailed();
        }
    }

    public async ValueTask ScheduleFromCommentAsync(PlayerId playerId, WorldId worldId, PostId sourcePostId, CommentId sourceCommentId)
    {
        var now = timeProvider.GetUtcNow();
        var result = await manager.AddAsync<GenerateStarFeedOnCommentPublishedJob, StarFeedOnCommentPublishedRequest>(
            now.UtcDateTime,
            new StarFeedOnCommentPublishedRequest(playerId, worldId, PostId.New(now), now.ToUnixTimeMilliseconds(), sourcePostId, sourceCommentId));
        if (!result.IsSucceeded)
        {
            SchedulingFailed();
        }
    }

    public async ValueTask ScheduleFromFeedAsync(PlayerId playerId, WorldId worldId)
    {
        if (Random.Shared.NextDouble() >= options.Value.StarFeedOpenProbability)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var result = await manager.AddAsync<GenerateStarFeedOnFeedOpenedJob, StarFeedRequest>(
            now.UtcDateTime,
            new StarFeedRequest(playerId, worldId, PostId.New(now), now.ToUnixTimeMilliseconds()));
        if (!result.IsSucceeded)
        {
            SchedulingFailed();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Star feed scheduling did not succeed")]
    private partial void SchedulingFailed();
}
