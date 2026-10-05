using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Telemetry;

public sealed record ReportRequest
{
    [Required]
    [MaxLength(64)]
    public required string CommentId { get; init; }

    [Required]
    [MaxLength(128)]
    public required string Reason { get; init; }
}

