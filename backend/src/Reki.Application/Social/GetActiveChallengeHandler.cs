using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

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

