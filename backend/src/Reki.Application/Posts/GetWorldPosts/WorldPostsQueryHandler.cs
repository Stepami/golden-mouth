using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.GetWorldPosts;

internal sealed class WorldPostsQueryHandler(
    IPostRepository postRepository,
    IRekiContextAccessor rekiContextAccessor,
    IStarFeedScheduler feedScheduler)
    : IQueryHandler<WorldPostsQuery, WorldPostsPage>
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

        return await postRepository.GetPostsAsync(
            world.Id,
            request.Feed ? null : world.Player.PlayerId.ToAuthorId(),
            request.Cursor,
            request.PageSize,
            cancellationToken);
    }
}
