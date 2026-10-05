using Reki.Domain.Game.Player;

namespace Reki.Application.Game.Player;

public sealed record PlayerView(PlayerProfile Player, int PostsLeft, int PostsCap);

