using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record AnswerEventRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string Text { get; init; }
}

