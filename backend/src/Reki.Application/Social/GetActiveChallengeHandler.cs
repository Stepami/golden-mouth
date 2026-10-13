using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class GetActiveChallengeHandler(
    IRekiContextAccessor contextAccessor,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<GetActiveChallengeQuery, ChallengeView?>
{
    public async ValueTask<ChallengeView?> Handle(
        GetActiveChallengeQuery request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        return await challengeLifecycle.GetActiveViewAsync(player.Id, cancellationToken);
    }
}

