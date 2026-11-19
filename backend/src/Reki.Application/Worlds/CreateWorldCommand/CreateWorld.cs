using Mediator;
using Reki.Application.Telemetry;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.CreateWorldCommand;

public sealed record CreateWorld(World Definition) : ICommand<ProductGoalResult<World>>;