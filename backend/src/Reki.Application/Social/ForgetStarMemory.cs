using Mediator;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social;

public sealed record ForgetStarMemory(StarId StarId, string Fact, WorldId WorldId) : IRequest;