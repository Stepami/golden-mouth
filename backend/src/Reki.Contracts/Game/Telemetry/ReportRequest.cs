using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record ReportRequest
{
    [Required]
    [MaxLength(64)]
    public required string CommentId { get; init; }

    [Required]
    [MaxLength(128)]
    public required string Reason { get; init; }
}

