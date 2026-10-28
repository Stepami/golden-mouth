using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI;

public sealed record StarFeedContext(
    StarAgentProfile Agent,
    IReadOnlyList<GamePost> OwnPosts,
    IReadOnlyList<GamePost> RecentPosts);
