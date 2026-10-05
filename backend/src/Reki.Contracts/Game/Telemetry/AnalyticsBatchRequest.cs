using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Game.Telemetry;

public sealed record AnalyticsBatchRequest
{
    [Required]
    [MaxLength(128)]
    public required string Session { get; init; }

    [Required]
    [MaxLength(64)]
    public required string Platform { get; init; }

    public IReadOnlyDictionary<string, string> Experiments { get; init; }
        = new Dictionary<string, string>();

    [Required]
    [MaxLength(100)]
    public required IReadOnlyList<AnalyticsEventRequest> Events { get; init; }
}

