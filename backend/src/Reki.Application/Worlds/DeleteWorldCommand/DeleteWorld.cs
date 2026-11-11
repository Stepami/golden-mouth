using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.DeleteWorldCommand;

public sealed record DeleteWorld(PlayerId PlayerId, WorldId WorldId) : ICommand;
