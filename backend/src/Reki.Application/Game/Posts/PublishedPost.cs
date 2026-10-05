using Reki.Domain.Game.Posts;

namespace Reki.Application.Game.Posts;

public sealed record PublishedPost(GamePost Post, string Hook, string StarName);

