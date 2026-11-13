using Reki.Application.Posts.GetWorldPosts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public interface IPostRepository
{
    ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<PostSimilarity>> FindSimilarPostsAsync(
        GamePost post, double threshold, CancellationToken cancellationToken);

    /// <summary>
    /// Reads posts in a world whose ownership the application has already verified.
    /// Returns posts ordered by descending UUID and the cursor for the next page, if any.
    /// </summary>
    ValueTask<WorldPostsPage> GetPostsAsync(
        WorldId worldId,
        AuthorId? authorId,
        PostId? cursor,
        int pageSize,
        AuthorId? likedBy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads a post only in the world whose ownership the application has already verified.
    /// </summary>
    ValueTask<GamePost?> GetPostAsync(
        PostId postId,
        WorldId worldId,
        CancellationToken cancellationToken);

    ValueTask<GamePost?> ToggleLikeAsync(PostId postId, WorldId worldId, AuthorId authorId, CancellationToken cancellationToken);

    /// <summary>Reads a story candidate; the caller must verify world ownership before exposing it.</summary>
    ValueTask<GamePost?> GetPublicStoryPostAsync(PostId postId, CancellationToken cancellationToken);

    ValueTask SaveReplyAsync(GamePost post, CancellationToken cancellationToken);

    ValueTask DeletePostsAsync(IReadOnlyCollection<WorldId> worldIds, CancellationToken cancellationToken);

    ValueTask DeletePostAsync(PostId postId, CancellationToken cancellationToken);
}
