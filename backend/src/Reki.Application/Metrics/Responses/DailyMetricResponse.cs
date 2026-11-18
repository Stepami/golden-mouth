using System.Text.Json.Serialization;

namespace Reki.Application.Metrics.Responses;

public sealed record DailyMetricResponse(
    DateOnly Date,
    long Dau,
    [property: JsonPropertyName("llm_requests")] long LlmRequests,
    [property: JsonPropertyName("tool_requests")] long ToolRequests,
    [property: JsonPropertyName("total_users")] long TotalUsers,
    [property: JsonPropertyName("new_users")] long NewUsers,
    [property: JsonPropertyName("is_final")] bool IsFinal);
