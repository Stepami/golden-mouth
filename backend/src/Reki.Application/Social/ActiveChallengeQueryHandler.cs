using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class ActiveChallengeQueryHandler(
    IRekiContextAccessor rekiContextAccessor,
    ChallengeLifecycle challengeLifecycle)
    : IQueryHandler<ActiveChallengeQuery, ChallengeView?>
{
    public async ValueTask<ChallengeView?> Handle(
        ActiveChallengeQuery request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        return await challengeLifecycle.GetActiveViewAsync(player.Id, cancellationToken);
    }
}

