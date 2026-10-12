using System.Text.Json.Serialization;

namespace Reki.Infrastructure.AI;

internal sealed record LlmSpendLog(
    [property: JsonPropertyName("request_id")] string? RequestId,
    [property: JsonPropertyName("spend")] double? Spend);
