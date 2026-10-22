using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record WorldTemplatesQuery : IQuery<IReadOnlyList<World>>;