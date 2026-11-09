using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.OnboardPlayerCommand;

public sealed record OnboardPlayer(
    PlayerId PlayerId,
    string BirthGroup,
    Address Address,
    string? ReferralCode,
    string? AgeBand) : ICommand<PlayerProfile>;
