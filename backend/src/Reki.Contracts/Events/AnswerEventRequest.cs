using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Events;

public sealed record AnswerEventRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string Text { get; init; }
}

