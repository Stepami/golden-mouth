namespace Reki.Domain.Posts;

public sealed record GameComment(
    CommentId Id,
    CommentId? ParentCommentId,
    string? RequestId,
    AuthorId AuthorId,
    string Text,
    bool Hidden,
    IReadOnlyList<CommentReaction> Reactions);