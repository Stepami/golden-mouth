using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Social;

public sealed record ForgetMemoryRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Fact { get; init; }
}

