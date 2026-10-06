using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record FeedView(PlayerProfile Player, IReadOnlyList<GamePost> Posts);
