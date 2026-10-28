using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI.Feed;

public sealed record StarFeedContext(
    StarAgentProfile Agent,
    IReadOnlyList<GamePost> OwnPosts,
    IReadOnlyList<GamePost> RecentPosts);
