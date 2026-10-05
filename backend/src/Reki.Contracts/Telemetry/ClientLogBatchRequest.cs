using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Telemetry;

public sealed record ClientLogBatchRequest
{
    [Required]
    [MaxLength(128)]
    public required string Session { get; init; }

    [Required]
    [MaxLength(100)]
    public required IReadOnlyList<ClientLogEntryRequest> Entries { get; init; }
}
