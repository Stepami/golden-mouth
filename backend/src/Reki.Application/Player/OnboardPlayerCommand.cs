using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Player;

public sealed record OnboardPlayerCommand(
    TelegramUserId TelegramUserId,
    string BirthGroup,
    WorldId World,
    string Role,
    string Name,
    string Handle,
    string Address,
    StarId Star) : IRequest<PlayerView>;

