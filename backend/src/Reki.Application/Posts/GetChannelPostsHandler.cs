using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
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
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        IReadOnlyList<GamePost> posts = await store.GetPostsAsync(
            player.Id,
            cancellationToken);
        return posts.Select(PostContractMapper.MapChannelPost).ToArray();
    }
}
