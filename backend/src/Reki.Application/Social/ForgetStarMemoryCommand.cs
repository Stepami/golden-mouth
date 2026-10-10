using Mediator;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed record ForgetStarMemoryCommand(StarId StarId, string Fact) : IRequest;

