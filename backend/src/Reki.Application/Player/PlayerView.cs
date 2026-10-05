using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record PlayerView(PlayerProfile Player, int PostsLeft, int PostsCap);

