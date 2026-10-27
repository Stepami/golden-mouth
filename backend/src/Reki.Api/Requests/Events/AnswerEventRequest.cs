using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Events;

public sealed record AnswerEventRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string Text { get; init; }
}

