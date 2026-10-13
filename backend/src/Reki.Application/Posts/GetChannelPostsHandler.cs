using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetChannelPostsHandler(IGameStore store, IRekiContextAccessor contextAccessor)
    : IRequestHandler<GetChannelPostsQuery, IReadOnlyList<GamePost>>
{
    public async ValueTask<IReadOnlyList<GamePost>> Handle(
        GetChannelPostsQuery request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        return await store.GetPostsAsync(request.Limit, cancellationToken, world.Id);
    }
}
