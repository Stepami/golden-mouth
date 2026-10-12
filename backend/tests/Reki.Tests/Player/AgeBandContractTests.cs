using System.ComponentModel.DataAnnotations;
using Reki.Contracts.Player;

namespace Reki.Tests.Player;

public sealed class AgeBandContractTests
{
    [Test]
    [Arguments(null, BirthGroup.Adult, true)]
    [Arguments("14-17", BirthGroup.Teen, true)]
    [Arguments("18-24", BirthGroup.Adult, true)]
    [Arguments("25-34", BirthGroup.Adult, true)]
    [Arguments("35-44", BirthGroup.Adult, true)]
    [Arguments("45+", BirthGroup.Adult, true)]
    [Arguments("18-24", BirthGroup.Teen, false)]
    [Arguments("14-17", BirthGroup.Adult, false)]
    [Arguments("24", BirthGroup.Adult, false)]
    public async Task Validate_OnlyConsistentCoarseBandsAreAcceptedAsync(string? band, BirthGroup group, bool expected)
    {
        var request = new OnboardingRequest
        {
            AgeBand = band,
            BirthGroup = group,
            Address = Address.Nick,
        };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        await Assert.That(valid).IsEqualTo(expected);
    }
}