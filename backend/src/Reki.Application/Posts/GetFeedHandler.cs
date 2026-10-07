using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetFeedHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetFeedQuery, IReadOnlyList<FeedItemResponse>>
{
    private const int FeedSize = 20;

    public async ValueTask<IReadOnlyList<FeedItemResponse>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        IReadOnlyList<GamePost> posts = await store.GetRecentWorldPostsAsync(
            player.World,
            FeedSize,
            cancellationToken);
        if (posts.Count == 0)
        {
            return [];
        }

        PlayerId[] authorIds = posts
            .Select(post => post.PlayerId)
            .Distinct()
            .ToArray();
        IReadOnlyList<PlayerProfile> authors = await store.GetPlayersAsync(
            authorIds,
            cancellationToken);
        Dictionary<PlayerId, PlayerProfile> authorsById = authors.ToDictionary(author => author.Id);
        return posts
            .Select(post => PostContractMapper.MapFeedItem(
                post,
                authorsById.TryGetValue(post.PlayerId, out PlayerProfile? author) ? author : player,
                post.PlayerId == player.Id))
            .ToArray();
    }
}
