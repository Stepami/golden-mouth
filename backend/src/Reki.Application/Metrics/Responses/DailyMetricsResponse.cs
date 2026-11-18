using System.Text.Json.Serialization;

namespace Reki.Application.Metrics.Responses;

public sealed record DailyMetricsResponse(
    [property: JsonPropertyName("startup_id")] string StartupId,
    string Timezone,
    [property: JsonPropertyName("generated_at")] DateTimeOffset GeneratedAt,
    IReadOnlyList<DailyMetricResponse> Data);
