using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record DirectMessageRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Text { get; init; }
}

