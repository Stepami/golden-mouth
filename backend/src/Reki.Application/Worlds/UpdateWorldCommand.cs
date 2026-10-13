using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record UpdateWorldCommand(WorldId WorldId, World Definition) : IRequest;
