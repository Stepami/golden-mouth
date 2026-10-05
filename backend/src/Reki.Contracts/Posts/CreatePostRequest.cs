using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Posts;

public sealed record CreatePostRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string Text { get; init; }
}

