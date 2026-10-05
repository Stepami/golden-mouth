using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record ClientLogEntryRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("t")]
    public long Timestamp { get; init; }

    public required ClientLogLevel Level { get; init; }

    [Required]
    [MaxLength(64)]
    public required string Event { get; init; }

    [MaxLength(64)]
    public string? Screen { get; init; }

    public IReadOnlyDictionary<string, JsonElement>? Props { get; init; }
}

