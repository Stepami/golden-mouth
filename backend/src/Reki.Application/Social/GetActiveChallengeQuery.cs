using Mediator;

namespace Reki.Application.Social;

public sealed record GetActiveChallengeQuery : IRequest<ChallengeView?>;

