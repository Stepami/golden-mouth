using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI;

public sealed record StarFeedCommentContext(
    StarFeedContext Feed,
    GamePost SourcePost,
    GameComment SourceComment,
    IReadOnlyList<GameComment> Branch,
    IReadOnlyList<GameComment> ExistingReplies);
