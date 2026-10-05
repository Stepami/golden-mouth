using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record ForgetMemoryRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Fact { get; init; }
}

