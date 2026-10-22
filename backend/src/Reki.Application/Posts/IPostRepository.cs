using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public interface IPostRepository
{
    ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken);

    /// <summary>
    /// Reads posts in a world whose ownership the application has already verified.
    /// Returns a page with its number and whether another page exists.
    /// </summary>
    ValueTask<WorldPostsPage> GetPostsAsync(
        WorldId worldId,
        AuthorId? authorId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads a post only in the world whose ownership the application has already verified.
    /// </summary>
    ValueTask<GamePost?> GetPostAsync(
        PostId postId,
        WorldId worldId,
        CancellationToken cancellationToken);

    /// <summary>Reads a story candidate; the caller must verify world ownership before exposing it.</summary>
    ValueTask<GamePost?> GetPublicStoryPostAsync(PostId postId, CancellationToken cancellationToken);
}
