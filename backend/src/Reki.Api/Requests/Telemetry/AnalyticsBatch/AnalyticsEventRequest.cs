using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Api.Requests.Telemetry.AnalyticsBatch;

public sealed record AnalyticsEventRequest
{
    /// <summary>
    /// Идентификатор экземпляра мира; отсутствует у событий до создания мира.
    /// </summary>
    [MaxLength(64)]
    [RegularExpression("^(?:[0-9a-fA-F]{32}|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$")]
    public string? WorldId { get; init; }

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