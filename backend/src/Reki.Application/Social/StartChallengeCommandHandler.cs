using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class StartChallengeCommandHandler(
    IRekiContextAccessor rekiContextAccessor,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<StartChallenge>
{
    public async ValueTask<Unit> Handle(
        StartChallenge request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        await challengeLifecycle.StartOrAcceptAsync(player, request.FriendId, cancellationToken);
        return Unit.Value;
    }
}

