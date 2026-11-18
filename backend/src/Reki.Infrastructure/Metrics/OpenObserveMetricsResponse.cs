using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reki.Infrastructure.Metrics;

public sealed record OpenObserveMetricsResponse(
    JsonElement[] Hits,
    [property: JsonPropertyName("is_partial")] bool IsPartial);
