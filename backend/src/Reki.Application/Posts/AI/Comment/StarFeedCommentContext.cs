using Reki.Application.Posts.AI.Feed;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI.Comment;

public sealed record StarFeedCommentContext(
    StarFeedContext Feed,
    GamePost SourcePost,
    GameComment SourceComment,
    IReadOnlyList<GameComment> Branch,
    IReadOnlyList<GameComment> ExistingReplies);
