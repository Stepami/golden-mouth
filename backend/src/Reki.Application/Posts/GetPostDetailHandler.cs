using Mediator;
using Reki.Application.Player;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed class GetPostDetailHandler(ISender sender) : IRequestHandler<GetPostDetailQuery, PostDetailResponse>
{
    public async ValueTask<PostDetailResponse> Handle(GetPostDetailQuery request, CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var world = await sender.Send(new Worlds.GetWorldQuery(player.Id, request.WorldId), cancellationToken);
        var post = await sender.Send(new GetPostQuery(request.PostId, player.Id, world.Id), cancellationToken);
        return PostContractMapper.MapPost(post, world, player.Id);
    }
}