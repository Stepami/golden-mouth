using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class StartChallengeHandler(
    IRekiContextAccessor rekiContextAccessor,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<StartChallengeCommand>
{
    public async ValueTask<Unit> Handle(
        StartChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        await challengeLifecycle.StartOrAcceptAsync(player, request.FriendId, cancellationToken);
        return Unit.Value;
    }
}

