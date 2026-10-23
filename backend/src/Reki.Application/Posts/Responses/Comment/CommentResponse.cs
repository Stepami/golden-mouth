using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses.Comment;

public sealed record CommentResponse(
    CommentId Id,
    string Author,
    string Avatar,
    string Handle,
    string Text,
    bool Star,
    bool Me,
    bool Hidden,
    CommentId? ParentCommentId,
    string? RequestId,
    AuthorId AuthorId,
    string Role,
    int Depth,
    IReadOnlyList<CommentReactionResponse> Reactions);