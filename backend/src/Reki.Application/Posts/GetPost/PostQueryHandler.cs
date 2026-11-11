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
        PostQuery query,
        CancellationToken cancellationToken)
    {
        var worlds = await rekiContextAccessor.Context.Worlds;
        var world = worlds.GetValueOrDefault(query.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        return await postRepository.GetPostAsync(query.PostId, world.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
    }
}
