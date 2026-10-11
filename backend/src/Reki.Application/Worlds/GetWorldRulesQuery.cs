using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record GetWorldRulesQuery(WorldId WorldId) : IRequest<WorldRules>;

