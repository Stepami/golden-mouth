using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Social;

public sealed record DirectMessageRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Text { get; init; }
}

