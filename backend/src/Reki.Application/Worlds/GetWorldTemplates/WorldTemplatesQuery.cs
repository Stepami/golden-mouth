using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.GetWorldTemplates;

public sealed record WorldTemplatesQuery : IQuery<IReadOnlyList<World>>;