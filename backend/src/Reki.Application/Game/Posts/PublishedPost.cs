using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record PublishedPost(GamePost Post, string Hook, string StarName);

