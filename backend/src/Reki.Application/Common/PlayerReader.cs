using Microsoft.Extensions.Options;
using Reki.Application.Common.Exceptions;
using Reki.Application.Player;
using Reki.Domain.Player;

namespace Reki.Application.Common;

public sealed class PlayerReader(IGameStore store, IOptions<GameBalance> balance)
{
    public async ValueTask<PlayerProfile> GetAsync(
        TelegramUserId telegramUserId,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await store.GetPlayerAsync(telegramUserId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
        return player.EnsureDefaults();
    }

    public PlayerView CreateView(PlayerProfile player)
    {
        int cap = player.GetPostsCap(balance.Value.FreePostsPerDay, balance.Value.PlusPostsPerDay);
        int postsLeft = Math.Max(0, cap - player.PostsUsed);
        return new PlayerView(player, postsLeft, cap);
    }
}
