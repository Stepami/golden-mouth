using System.ComponentModel.DataAnnotations;
using Reki.Domain.Player;

namespace Reki.Api.Requests.Player;

public sealed record OnboardingRequest : IValidatableObject
{
    [MaxLength(64)]
    public string? ReferralCode { get; init; }

    public required int BirthYear { get; init; }

    public required Address Address { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currentYear = validationContext.GetRequiredService<TimeProvider>().GetUtcNow().Year;
        if (BirthYear > currentYear - 18)
        {
            yield return new ValidationResult("New accounts require age 18 or older.", [nameof(BirthYear)]);
        }
    }
}
