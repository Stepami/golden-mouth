using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Posts;

public sealed class GetFeedHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetFeedQuery, IReadOnlyList<FeedItemResponse>>
{
    private const int FeedSize = 20;

    public async ValueTask<IReadOnlyList<FeedItemResponse>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        IReadOnlyList<GamePost> posts = await store.GetRecentPostsAsync(
            player.Id,
            FeedSize,
            cancellationToken);
        return posts
            .Select(post => PostContractMapper.MapFeedItem(post, player, true))
            .ToArray();
    }
}
