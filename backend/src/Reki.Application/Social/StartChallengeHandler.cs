using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class StartChallengeHandler(
    PlayerReader playerReader,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<StartChallengeCommand>
{
    public async ValueTask<Unit> Handle(
        StartChallengeCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        await challengeLifecycle.StartOrAcceptAsync(player, request.FriendId, cancellationToken);
        return Unit.Value;
    }
}

