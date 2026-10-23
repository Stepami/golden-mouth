using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.RekiContext;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.GetPost;

internal sealed class PostQueryHandler(
    IPostRepository postRepository,
    IRekiContextAccessor rekiContextAccessor)
    : IQueryHandler<PostQuery, GamePost>
{
    public async ValueTask<GamePost> Handle(
        PostQuery request,
        CancellationToken cancellationToken)
    {
        var worlds = await rekiContextAccessor.Context.Worlds;
        var world = worlds.GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        return await postRepository.GetPostAsync(request.PostId, world.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
    }
}
