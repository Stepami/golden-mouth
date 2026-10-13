using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class GetActiveChallengeHandler(
    IRekiContextAccessor rekiContextAccessor,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<GetActiveChallengeQuery, ChallengeView?>
{
    public async ValueTask<ChallengeView?> Handle(
        GetActiveChallengeQuery request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        return await challengeLifecycle.GetActiveViewAsync(player.Id, cancellationToken);
    }
}

