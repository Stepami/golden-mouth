using System.ComponentModel.DataAnnotations;
using Reki.Domain.Posts;

namespace Reki.Api.Requests.Telemetry;

public sealed record ReportRequest
{
    [Required]
    public required CommentId CommentId { get; init; }

    [Required]
    [MaxLength(128)]
    public required string Reason { get; init; }
}

