using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.UpdateWorldCommand;

public sealed record UpdateWorld(WorldId WorldId, World Definition) : ICommand;
