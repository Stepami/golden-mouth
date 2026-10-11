using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record CreateWorldCommand(World Definition) : IRequest<World>;