using System.ComponentModel.DataAnnotations;
using Reki.Application.Telemetry;
using Reki.Domain.Telemetry;

namespace Reki.Api.Requests.Telemetry.AnalyticsBatch;

public sealed record AnalyticsBatchRequest : IValidatableObject
{
    [Required]
    public required SessionId Session { get; init; }

    [Required]
    [MaxLength(64)]
    public required string Platform { get; init; }

    [EnumDataType(typeof(LaunchSurface))]
    public LaunchSurface LaunchSurface { get; init; } = LaunchSurface.Unknown;

    public IReadOnlyDictionary<string, string> Experiments { get; init; }
        = new Dictionary<string, string>();

    [Required]
    [MaxLength(100)]
    public required IReadOnlyList<AnalyticsEventRequest> Events { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Events?.Any(entry => entry is null) is true)
        {
            yield return new ValidationResult("Events must not contain null items.", [nameof(Events)]);
        }
    }
}
