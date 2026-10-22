using Reki.Application.Common.Balance;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Safety;

public static class StarTurnPolicy
{
    public static bool IsHateEnabled(PlayerProfile player, World gameWorld, GameBalance balance, DateTimeOffset now)
    {
        return balance.Worlds.TryGetValue(gameWorld.BalanceKey, out var world) &&
            gameWorld.Player.Subscribers >= world.HateFrom && gameWorld.Player.Drama != WorldDrama.Off &&
            (player.HateDisabledUntilUtc is null || player.HateDisabledUntilUtc <= now);
    }

    public static void EnsurePostAllowed(GeneratedPostTurn turn, World world, bool hateEnabled)
    {
        if (!world.HasStar(turn.StarId, turn.StarName) ||
            turn.Tone is not ("support" or "critique" or "hate") ||
            (turn.Tone == "hate" && !hateEnabled))
        {
            throw new AiServiceUnavailableException("The model returned a forbidden identity or tone.");
        }
    }
}