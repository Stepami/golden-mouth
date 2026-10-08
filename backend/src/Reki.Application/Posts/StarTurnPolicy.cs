using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Social;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public static class StarTurnPolicy
{
    public static bool IsHateEnabled(PlayerProfile player, GameBalance balance, DateTimeOffset now)
    {
        return balance.Worlds.TryGetValue(player.World.Value, out WorldBalance? world) &&
            player.Subscribers >= world.HateFrom && player.Settings.Drama != "off" &&
            (player.HateDisabledUntilUtc is null || player.HateDisabledUntilUtc <= now);
    }

    public static void EnsurePostAllowed(GeneratedPostTurn turn, WorldId world, bool hateEnabled)
    {
        if (!StarCatalog.Matches(world, turn.StarId, turn.StarName) ||
            turn.Tone is not ("support" or "critique" or "hate") ||
            (turn.Tone == "hate" && !hateEnabled))
        {
            throw new AiServiceUnavailableException("The model returned a forbidden identity or tone.");
        }
    }
}
