using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.CreateWorldCommand;

public sealed record CreateWorld(World Definition) : ICommand<World>;