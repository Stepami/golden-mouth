using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed class GetPostDetailHandler(ISender sender, IRekiContextAccessor rekiContextAccessor) : IRequestHandler<GetPostDetailQuery, PostDetailResponse>
{
    public async ValueTask<PostDetailResponse> Handle(GetPostDetailQuery request, CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new GetPostQuery(request.PostId, world.Id), cancellationToken);
        return PostContractMapper.MapPost(post, world, player.Id);
    }
}