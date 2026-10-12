using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record GetWorldTemplatesQuery : IRequest<IReadOnlyList<World>>;