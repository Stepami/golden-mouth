using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Social;

public sealed record DirectMessageRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Text { get; init; }
}

