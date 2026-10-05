using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed class GetActiveChallengeHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetActiveChallengeQuery, GameChallenge?>
{
    public async ValueTask<GameChallenge?> Handle(
        GetActiveChallengeQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        return await store.GetActiveChallengeAsync(player.Id, cancellationToken);
    }
}

