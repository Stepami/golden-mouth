using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI.Feed;

public sealed record StarFeedSourceComment(
    CommentId CommentId,
    CommentId? ParentCommentId,
    AuthorId AuthorId,
    string Text);
