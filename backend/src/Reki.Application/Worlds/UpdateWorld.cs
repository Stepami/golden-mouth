using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record UpdateWorld(WorldId WorldId, World Definition) : IRequest;
