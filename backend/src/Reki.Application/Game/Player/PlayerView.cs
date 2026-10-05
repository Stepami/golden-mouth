using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record PlayerView(PlayerProfile Player, int PostsLeft, int PostsCap);

