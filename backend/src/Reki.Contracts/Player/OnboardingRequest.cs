using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Player;

public sealed record OnboardingRequest
{
    [MaxLength(64)]
    public string? ReferralCode { get; init; }

    public required BirthGroup BirthGroup { get; init; }

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
}
