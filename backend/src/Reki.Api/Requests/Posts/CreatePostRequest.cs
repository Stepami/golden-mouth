using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Posts;

public sealed record CreatePostRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string Text { get; init; }

    public bool OwnWords { get; init; } = true;

    [StringLength(64)]
    public string? WantId { get; init; }
}
