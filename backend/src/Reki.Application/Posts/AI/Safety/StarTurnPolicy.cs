using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Safety;

internal sealed class StarTurnPolicy(IOptionsMonitor<GameBalance> balance, TimeProvider timeProvider)
{
    public bool IsHateEnabled(PlayerProfile player, World gameWorld)
    {
        var currentBalance = balance.CurrentValue;
        var profile = currentBalance.Difficulties[gameWorld.Difficulty];
        return gameWorld.Player.Subscribers >= profile.HateFrom && gameWorld.Player.Drama != WorldDrama.Off &&
            (player.HateDisabledUntilUtc is null || player.HateDisabledUntilUtc <= timeProvider.GetUtcNow());
    }
}
