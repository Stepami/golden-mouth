using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI;

public sealed record StarFeedSourceContent(
    PostId PostId,
    AuthorId AuthorId,
    string Text,
    StarFeedSourceComment? Comment,
    IReadOnlyList<StarFeedSourceComment> Branch,
    IReadOnlyList<StarFeedSourceComment> ExistingReplies);

