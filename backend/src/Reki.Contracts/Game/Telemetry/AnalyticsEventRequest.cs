using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record AnalyticsEventRequest
{
    [Required]
    [MaxLength(64)]
    public required string Name { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("t")]
    public long Timestamp { get; init; }

    [MaxLength(64)]
    public string Screen { get; init; } = string.Empty;

    [MaxLength(64)]
    public string? Prev { get; init; }

    public IReadOnlyDictionary<string, JsonElement>? Props { get; init; }
}

