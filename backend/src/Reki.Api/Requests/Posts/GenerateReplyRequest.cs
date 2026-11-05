using Reki.Domain.Posts;

namespace Reki.Api.Requests.Posts;

public sealed record GenerateReplyRequest
{
    public AuthorId? AuthorId { get; init; }

    public CommentId? SourceCommentId { get; init; }

    public required bool Additional { get; init; }
}
