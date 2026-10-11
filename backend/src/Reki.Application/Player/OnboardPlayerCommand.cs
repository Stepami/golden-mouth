using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record OnboardPlayerCommand(
    string BirthGroup,
    string Address,
    string? ReferralCode,
    string? AgeBand) : IRequest<PlayerProfile>;