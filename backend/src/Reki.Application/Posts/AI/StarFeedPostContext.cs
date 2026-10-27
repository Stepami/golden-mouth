using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI;

public sealed record StarFeedPostContext(
    StarFeedContext Feed,
    GamePost SourcePost,
    IReadOnlyList<GameComment> ExistingReplies);
