using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Posts;

public sealed record ReplyRequest
{
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public required string CommentId { get; init; }

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public required string RequestId { get; init; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Text { get; init; }
}