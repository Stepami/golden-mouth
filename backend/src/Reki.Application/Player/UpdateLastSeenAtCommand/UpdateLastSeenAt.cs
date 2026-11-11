using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.UpdateLastSeenAtCommand;

public sealed record UpdateLastSeenAt(PlayerProfile Player) : ICommand;
