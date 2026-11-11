using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.GetWorldPosts;

internal sealed class WorldPostsQueryHandler(
    IPostRepository postRepository,
    IRekiContextAccessor rekiContextAccessor,
    IStarFeedScheduler<ScheduleFromFeedRequest> feedScheduler)
    : IQueryHandler<WorldPostsQuery, WorldPostsPage>
{
    public async ValueTask<WorldPostsPage> Handle(
        WorldPostsQuery query,
        CancellationToken cancellationToken)
    {
        var context = rekiContextAccessor.Context;
        var worlds = await context.Worlds;
        var world = worlds.GetValueOrDefault(query.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");

        if (query.Feed)
        {
            await feedScheduler.ScheduleFeedAsync(new ScheduleFromFeedRequest(
                world.Player.PlayerId,
                world.Id));
        }

        return await postRepository.GetPostsAsync(
            world.Id,
            query.Feed ? null : world.Player.PlayerId.ToAuthorId(),
            query.Cursor,
            query.PageSize,
            cancellationToken);
    }
}
