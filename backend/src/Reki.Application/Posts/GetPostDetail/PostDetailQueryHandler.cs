using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Posts.GetPost;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts.GetPostDetail;

public sealed class PostDetailQueryHandler(ISender sender, IRekiContextAccessor rekiContextAccessor) : IQueryHandler<PostDetailQuery, PostDetailResponse>
{
    public async ValueTask<PostDetailResponse> Handle(PostDetailQuery request, CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(request.PostId, world.Id), cancellationToken);
        return PostContractMapper.MapPost(post, world, player.Id);
    }
}