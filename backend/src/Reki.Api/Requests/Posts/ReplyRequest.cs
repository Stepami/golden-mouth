using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Posts;

public sealed record ReplyRequest
{
    [StringLength(64, MinimumLength = 1)]
    public string? CommentId { get; init; }

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public required string RequestId { get; init; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Text { get; init; }
}
