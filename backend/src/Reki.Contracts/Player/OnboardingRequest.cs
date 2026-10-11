using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Player;

public sealed record OnboardingRequest : IValidatableObject
{
    [MaxLength(64)]
    public string? ReferralCode { get; init; }

    public required BirthGroup BirthGroup { get; init; }

    [RegularExpression("^(14-17|18-24|25-34|35-44|45\\+)$")]
    public string? AgeBand { get; init; }

    public required WorldId World { get; init; }

    [Required]
    [MaxLength(80)]
    public required string Role { get; init; }

    [Required]
    [MaxLength(80)]
    public required string Name { get; init; }

    [Required]
    [MaxLength(80)]
    public required string Handle { get; init; }

    public required Address Address { get; init; }

    [Required]
    [MaxLength(64)]
    public required string Star { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AgeBand is not null && ((AgeBand == "14-17") != (BirthGroup == BirthGroup.Teen)))
        {
            yield return new ValidationResult("Age band must match birth group.", [nameof(AgeBand)]);
        }
    }
}
