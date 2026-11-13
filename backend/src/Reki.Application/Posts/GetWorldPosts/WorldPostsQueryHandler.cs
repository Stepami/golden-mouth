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

        var authors = world.Stars
            .Select(star => star.Id.ToAuthorId())
            .Append(world.Player.PlayerId.ToAuthorId())
            .ToHashSet();
        var isFeed = authors.SetEquals(query.AuthorIds);
        if (isFeed)
        {
            await feedScheduler.ScheduleFeedAsync(new ScheduleFromFeedRequest(
                world.Player.PlayerId,
                world.Id));
        }

        return await postRepository.GetPostsAsync(
            world.Id,
            isFeed ? [] : query.AuthorIds,
            query.Cursor,
            query.PageSize,
            query.LikedBy,
            cancellationToken);
    }
}
