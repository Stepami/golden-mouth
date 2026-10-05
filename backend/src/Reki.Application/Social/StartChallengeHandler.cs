using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class StartChallengeHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<StartChallengeCommand>
{
    public async ValueTask<Unit> Handle(
        StartChallengeCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        GameChallenge? active = await store.GetActiveChallengeAsync(player.Id, cancellationToken);
        if (active is not null)
        {
            throw new RequestConflictException("An active challenge already exists.");
        }

        string challengeId = Guid.NewGuid().ToString("N");
        var challenge = new GameChallenge(
                challengeId,
                player.Id,
                request.FriendId,
                1,
                3,
                0,
                0,
                string.Empty);
        await store.SaveChallengeAsync(challenge, cancellationToken);
        return Unit.Value;
    }
}

