using Reki.Application.Posts.AI.Feed;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI.Post;

public sealed record StarFeedPostContext(
    StarFeedContext Feed,
    GamePost SourcePost,
    IReadOnlyList<GameComment> ExistingReplies);
