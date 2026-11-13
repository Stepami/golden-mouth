using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.GetPost;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.GetPostDetail;

internal sealed class PostDetailQueryHandler(ISender sender, IRekiContextAccessor rekiContextAccessor, IPostRepository posts) : IQueryHandler<PostDetailQuery, PostDetailResponse>
{
    public async ValueTask<PostDetailResponse> Handle(PostDetailQuery query, CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var world = (await rekiContext.Worlds).GetValueOrDefault(query.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(query.PostId, world.Id), cancellationToken);
        var source = post.QuotedPostId is { } id ? await posts.GetPostAsync(id, world.Id, cancellationToken) : null;
        return post.MapPost(world, rekiContext.PlayerId, source);
    }
}