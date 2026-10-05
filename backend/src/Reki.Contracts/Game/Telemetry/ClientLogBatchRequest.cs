using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record ClientLogBatchRequest
{
    [Required]
    [MaxLength(128)]
    public required string Session { get; init; }

    [Required]
    [MaxLength(100)]
    public required IReadOnlyList<ClientLogEntryRequest> Entries { get; init; }
}
