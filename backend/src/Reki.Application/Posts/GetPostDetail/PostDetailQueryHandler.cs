using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.GetPost;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.GetPostDetail;

internal sealed class PostDetailQueryHandler(ISender sender, IRekiContextAccessor rekiContextAccessor) : IQueryHandler<PostDetailQuery, PostDetailResponse>
{
    public async ValueTask<PostDetailResponse> Handle(PostDetailQuery request, CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(request.PostId, world.Id), cancellationToken);
        return post.MapPost(world, player.Id);
    }
}