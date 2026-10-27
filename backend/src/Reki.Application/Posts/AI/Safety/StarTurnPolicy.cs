using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Safety;

internal sealed class StarTurnPolicy(TimeProvider timeProvider)
{
    public bool IsHateEnabled(PlayerProfile player, World gameWorld, GameBalance currentBalance)
    {
        var profile = currentBalance.Difficulties[gameWorld.Difficulty];
        return gameWorld.Player.Subscribers >= profile.HateFrom && gameWorld.Player.Drama != WorldDrama.Off &&
            (player.HateDisabledUntilUtc is null || player.HateDisabledUntilUtc <= timeProvider.GetUtcNow());
    }

    public void EnsurePostAllowed(GeneratedPostTurn turn, PlayerProfile player, World world, GameBalance currentBalance)
    {
        if (!world.HasStar(turn.StarId, turn.StarName) ||
            turn.Tone is not ("support" or "critique" or "hate") ||
            (turn.Tone == "hate" && !IsHateEnabled(player, world, currentBalance)))
        {
            throw new AiServiceUnavailableException("The model returned a forbidden identity or tone.");
        }
    }
}
