using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.GetPostDetail;

internal sealed class PostDetailQueryHandler(
    IPostRepository posts,
    IRekiContextAccessor rekiContextAccessor) : IQueryHandler<PostDetailQuery, PostDetailResponse?>
{
    public async ValueTask<PostDetailResponse?> Handle(PostDetailQuery query, CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var world = (await rekiContext.Worlds).GetValueOrDefault(query.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await posts.GetPostAsync(query.PostId, cancellationToken);
        return post?.MapPost(world, rekiContext.PlayerId);
    }
}
