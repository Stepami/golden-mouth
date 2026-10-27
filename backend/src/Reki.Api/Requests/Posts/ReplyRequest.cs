using System.ComponentModel.DataAnnotations;
using Reki.Domain.Posts;

namespace Reki.Api.Requests.Posts;

public sealed record ReplyRequest
{
    public CommentId? CommentId { get; init; }

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public required string RequestId { get; init; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Text { get; init; }
}
