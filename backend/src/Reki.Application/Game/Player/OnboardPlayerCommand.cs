using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record OnboardPlayerCommand(
    long TelegramUserId,
    string BirthGroup,
    string World,
    string Role,
    string Avatar,
    string Name,
    string Handle,
    string Address,
    string Star) : IRequest<PlayerView>;

