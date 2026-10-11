using Microsoft.Extensions.Options;
using Reki.Application.Player;
using Reki.Domain.Player;

namespace Reki.Application.Common;

public sealed class PlayerReader(IOptions<GameBalance> balance)
{
    public PlayerView CreateView(PlayerProfile player)
    {
        var cap = player.GetPostsCap(balance.Value.FreePostsPerDay, balance.Value.PlusPostsPerDay);
        var postsLeft = Math.Max(0, cap - player.PostsUsed);
        return new PlayerView(player, postsLeft, cap);
    }
}
