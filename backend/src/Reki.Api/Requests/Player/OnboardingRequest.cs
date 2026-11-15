using System.ComponentModel.DataAnnotations;
using Reki.Domain.Player;

namespace Reki.Api.Requests.Player;

public sealed record OnboardingRequest : IValidatableObject
{
    [MaxLength(64)]
    public string? ReferralCode { get; init; }

    public required BirthGroup BirthGroup { get; init; }

    [RegularExpression("^(14-17|18-24|25-34|35-44|45\\+)$")]
    public string? AgeBand { get; init; }

    public required Address Address { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BirthGroup != BirthGroup.Adult)
        {
            yield return new ValidationResult("New accounts require age 18 or older.", [nameof(BirthGroup)]);
        }

        if (AgeBand is not null && ((AgeBand == "14-17") != (BirthGroup == BirthGroup.Teen)))
        {
            yield return new ValidationResult("Age band must match birth group.", [nameof(AgeBand)]);
        }
    }
}
