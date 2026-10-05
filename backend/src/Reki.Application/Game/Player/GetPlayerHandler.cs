using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

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

