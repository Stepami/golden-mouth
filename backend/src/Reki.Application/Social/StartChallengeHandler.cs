using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class StartChallengeHandler(
    IRekiContextAccessor contextAccessor,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<StartChallengeCommand>
{
    public async ValueTask<Unit> Handle(
        StartChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        await challengeLifecycle.StartOrAcceptAsync(player, request.FriendId, cancellationToken);
        return Unit.Value;
    }
}

