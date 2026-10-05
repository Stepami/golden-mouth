using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Social;

public sealed record ForgetMemoryRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Fact { get; init; }
}

