using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record CreateWorld(World Definition) : IRequest<World>;