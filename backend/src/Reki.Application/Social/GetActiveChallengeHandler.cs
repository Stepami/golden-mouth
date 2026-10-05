using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class GetActiveChallengeHandler(
    PlayerReader playerReader,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<GetActiveChallengeQuery, ChallengeView?>
{
    public async ValueTask<ChallengeView?> Handle(
        GetActiveChallengeQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        return await challengeLifecycle.GetActiveViewAsync(player.Id, cancellationToken);
    }
}

