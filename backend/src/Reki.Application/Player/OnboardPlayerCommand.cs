using Mediator;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Player;

public sealed record OnboardPlayerCommand(
    string BirthGroup,
    WorldId World,
    string Role,
    string Name,
    string Handle,
    string Address,
    StarId Star,
    string? ReferralCode,
    string? AgeBand) : IRequest<PlayerView>;

