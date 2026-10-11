using Mediator;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social;

public sealed record ForgetStarMemoryCommand(StarId StarId, string Fact, WorldId WorldId) : IRequest;