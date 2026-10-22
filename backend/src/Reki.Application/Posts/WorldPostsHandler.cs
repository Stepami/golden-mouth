using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;

namespace Reki.Application.Posts;

public sealed class WorldPostsHandler(
    IGameStore store,
    IRekiContextAccessor rekiContextAccessor,
    IStarFeedScheduler feedScheduler)
    : IRequestHandler<WorldPostsQuery, WorldPostsPage>
{
    public async ValueTask<WorldPostsPage> Handle(
        WorldPostsQuery request,
        CancellationToken cancellationToken)
    {
        var context = rekiContextAccessor.Context;
        var worlds = await context.Worlds;
        var world = worlds.GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");

        if (request.Feed)
        {
            await feedScheduler.ScheduleFromFeedAsync(
                world.Player.PlayerId,
                world.Id);
        }

        var posts = await store.GetPostsAsync(
            world.Id,
            request.Feed ? null : world.Player.PlayerId.ToAuthorId(),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
        var hasNext = posts.Count > request.PageSize;
        if (hasNext)
        {
            posts.RemoveAt(posts.Count - 1);
        }

        return new WorldPostsPage(posts, request.PageNumber, hasNext);
    }
}
