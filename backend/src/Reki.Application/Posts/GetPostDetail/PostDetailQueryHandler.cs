using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.GetPost;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.GetPostDetail;

internal sealed class PostDetailQueryHandler(ISender sender, IRekiContextAccessor rekiContextAccessor) : IQueryHandler<PostDetailQuery, PostDetailResponse?>
{
    public async ValueTask<PostDetailResponse?> Handle(PostDetailQuery query, CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var world = (await rekiContext.Worlds).GetValueOrDefault(query.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(query.PostId), cancellationToken);
        return post?.WorldId == world.Id ? post.MapPost(world, rekiContext.PlayerId) : null;
    }
}
