using Mediator;
using Reki.Application.Game.Common;
using Reki.Domain.Game.Player;

namespace Reki.Application.Game.Player;

public sealed class GetPlayerHandler(
    IGameStore store,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<GetPlayerQuery, PlayerView>
{
    public async ValueTask<PlayerView> Handle(
        GetPlayerQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        PlayerProfile normalized = gameDay.Normalize(player);

        if (!ReferenceEquals(player, normalized))
        {
            await store.UpsertPlayerAsync(normalized, cancellationToken);
        }

        return playerReader.CreateView(normalized);
    }
}

