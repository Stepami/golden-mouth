using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetChannelPostsHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetChannelPostsQuery, IReadOnlyList<ChannelPostResponse>>
{
    public async ValueTask<IReadOnlyList<ChannelPostResponse>> Handle(
        GetChannelPostsQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        IReadOnlyList<GamePost> posts = await store.GetPostsAsync(
            player.Id,
            cancellationToken);
        return posts.Select(PostContractMapper.MapChannelPost).ToArray();
    }
}
