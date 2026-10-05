using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed class PlayerReader(IGameStore store, GameBalance balance)
{
    public async ValueTask<PlayerProfile> GetAsync(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        return await store.GetPlayerAsync(telegramUserId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
    }

    public PlayerView CreateView(PlayerProfile player)
    {
        int cap = player.GetPostsCap(balance.FreePostsPerDay, balance.PlusPostsPerDay);
        int postsLeft = Math.Max(0, cap - player.PostsUsed);
        return new PlayerView(player, postsLeft, cap);
    }
}
