using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetChannelPostsHandler(
    IGameStore store,
    IRekiContextAccessor rekiContextAccessor,
    IStarFeedScheduler feedScheduler)
    : IRequestHandler<GetChannelPostsQuery, IReadOnlyList<GamePost>>
{
    public async ValueTask<IReadOnlyList<GamePost>> Handle(
        GetChannelPostsQuery request,
        CancellationToken cancellationToken)
    {
        var worlds = await rekiContextAccessor.Context.Worlds;
        var world = worlds.GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");

        if (request.Feed)
        {
            await feedScheduler.ScheduleFromFeedAsync(
                world.Player.PlayerId,
                world.Id);
        }

        return await store.GetPostsAsync(
            request.Limit,
            world.Id,
            request.Feed ? null : world.Player.PlayerId.ToAuthorId(),
            cancellationToken);
    }
}
