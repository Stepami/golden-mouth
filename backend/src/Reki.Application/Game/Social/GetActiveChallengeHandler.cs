using Mediator;
using Reki.Application.Game.Common;
using Reki.Domain.Game.Player;
using Reki.Domain.Game.Social;

namespace Reki.Application.Game.Social;

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

