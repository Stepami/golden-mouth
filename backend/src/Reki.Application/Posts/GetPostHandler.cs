using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetPostHandler(IGameStore store, ISender sender)
    : IRequestHandler<GetPostQuery, GamePost>
{
    public async ValueTask<GamePost> Handle(
        GetPostQuery request,
        CancellationToken cancellationToken)
    {
        var post = await store.GetPostAsync(request.PostId, request.PlayerId, cancellationToken)
                   ?? throw new ResourceNotFoundException("Post not found.");
        var world = await sender.Send(new Reki.Application.Worlds.GetWorldQuery(request.PlayerId, request.WorldId), cancellationToken);
        if (post.WorldId != world.Id)
        {
            throw new ResourceNotFoundException("Post not found.");
        }

        return post;
    }
}