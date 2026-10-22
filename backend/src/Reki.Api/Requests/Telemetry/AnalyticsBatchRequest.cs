using System.ComponentModel.DataAnnotations;
using Reki.Application.Telemetry;

namespace Reki.Api.Requests.Telemetry;

public sealed record AnalyticsBatchRequest
{
    [Required]
    [MaxLength(128)]
    public required string Session { get; init; }

    [Required]
    [MaxLength(64)]
    public required string Platform { get; init; }

    [EnumDataType(typeof(LaunchSurface))]
    public LaunchSurface LaunchSurface { get; init; }

    public IReadOnlyDictionary<string, string> Experiments { get; init; }
        = new Dictionary<string, string>();

    [Required]
    [MaxLength(100)]
    public required IReadOnlyList<AnalyticsEventRequest> Events { get; init; }
}

