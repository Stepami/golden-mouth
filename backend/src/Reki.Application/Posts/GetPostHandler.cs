using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetPostHandler(IGameStore store, IRekiContextAccessor contextAccessor)
    : IRequestHandler<GetPostQuery, GamePost>
{
    public async ValueTask<GamePost> Handle(
        GetPostQuery request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        return await store.GetPostAsync(request.PostId, world.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
    }
}
