using System.Text.Json.Serialization;

namespace Reki.Infrastructure.Metrics;

public sealed record OpenObserveMetricsQuery(
    string Sql,
    [property: JsonPropertyName("start_time")] long StartTime,
    [property: JsonPropertyName("end_time")] long EndTime,
    int From,
    int Size);
