using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Game.Posts;

public sealed record ReplyRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Text { get; init; }
}

