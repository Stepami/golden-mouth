using Mediator;
using Reki.Application.Player;

namespace Reki.Application.Social;

public sealed class StartChallengeHandler(
    ISender sender,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<StartChallengeCommand>
{
    public async ValueTask<Unit> Handle(
        StartChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        await challengeLifecycle.StartOrAcceptAsync(player, request.FriendId, cancellationToken);
        return Unit.Value;
    }
}

