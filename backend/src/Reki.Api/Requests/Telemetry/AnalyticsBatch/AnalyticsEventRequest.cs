using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Reki.Domain.Worlds;

namespace Reki.Api.Requests.Telemetry.AnalyticsBatch;

public sealed record AnalyticsEventRequest
{
    /// <summary>
    /// Идентификатор экземпляра мира; отсутствует у событий до создания мира.
    /// </summary>
    public WorldId? WorldId { get; init; }

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
