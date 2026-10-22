using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record WorldPostsPage(IReadOnlyList<GamePost> Items, int PageNumber, bool HasNext);
