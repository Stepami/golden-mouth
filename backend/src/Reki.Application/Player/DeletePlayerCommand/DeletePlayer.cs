using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.DeletePlayerCommand;

public sealed record DeletePlayer(PlayerId PlayerId) : ICommand;

