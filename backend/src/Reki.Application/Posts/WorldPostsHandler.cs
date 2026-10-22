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
        ArgumentOutOfRangeException.ThrowIfNegative(request.PageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.PageSize);
        var offset = checked(request.PageNumber * request.PageSize);
        var limit = checked(request.PageSize + 1);
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
            offset,
            limit,
            cancellationToken);
        return new WorldPostsPage(
            posts.Take(request.PageSize).ToArray(),
            request.PageNumber,
            posts.Count > request.PageSize);
    }
}
