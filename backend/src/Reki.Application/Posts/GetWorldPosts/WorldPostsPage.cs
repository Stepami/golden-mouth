using Reki.Domain.Posts;

namespace Reki.Application.Posts.GetWorldPosts;

public sealed record WorldPostsPage(IReadOnlyList<GamePost> Items, PostId? NextCursor);
