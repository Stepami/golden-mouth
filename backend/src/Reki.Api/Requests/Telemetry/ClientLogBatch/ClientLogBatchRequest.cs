using System.ComponentModel.DataAnnotations;
using Reki.Domain.Telemetry;

namespace Reki.Api.Requests.Telemetry.ClientLogBatch;

public sealed record ClientLogBatchRequest
{
    [Required]
    public required SessionId Session { get; init; }

    [Required]
    [MaxLength(100)]
    public required IReadOnlyList<ClientLogEntryRequest> Entries { get; init; }
}
