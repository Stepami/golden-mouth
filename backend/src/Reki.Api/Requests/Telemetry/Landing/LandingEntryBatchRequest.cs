using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Telemetry.Landing;

public sealed record LandingEntryBatchRequest : IValidatableObject
{
    [Required]
    [MinLength(1)]
    [MaxLength(5)]
    public required IReadOnlyList<LandingEntryEventRequest> Events { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Events?.Any(entry => entry is null) is true)
        {
            yield return new ValidationResult("Events must not contain null items.", [nameof(Events)]);
        }
    }
}
