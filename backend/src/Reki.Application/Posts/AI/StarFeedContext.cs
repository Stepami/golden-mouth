using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI;

public sealed record StarFeedContext(
    StarAgentProfile Agent,
    StarFeedEvent SourceEvent,
    IReadOnlyList<GamePost> OwnPosts,
    IReadOnlyList<GamePost> RecentPosts,
    GamePost? SourcePost,
    GameComment? SourceComment,
    IReadOnlyList<GameComment> Branch,
    IReadOnlyList<GameComment> ExistingReplies);
