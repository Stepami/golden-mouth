using Mediator;
using Reki.Application.Player;

namespace Reki.Application.Social;

public sealed class GetActiveChallengeHandler(
    ISender sender,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<GetActiveChallengeQuery, ChallengeView?>
{
    public async ValueTask<ChallengeView?> Handle(
        GetActiveChallengeQuery request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        return await challengeLifecycle.GetActiveViewAsync(player.Id, cancellationToken);
    }
}

