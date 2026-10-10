using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Posts;

public sealed class GetChannelPostsHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetChannelPostsQuery, IReadOnlyList<ChannelPostResponse>>
{
    public async ValueTask<IReadOnlyList<ChannelPostResponse>> Handle(
        GetChannelPostsQuery request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        var posts = await store.GetPostsAsync(
            player.Id,
            cancellationToken);
        return posts.Select(PostContractMapper.MapChannelPost).ToArray();
    }
}
