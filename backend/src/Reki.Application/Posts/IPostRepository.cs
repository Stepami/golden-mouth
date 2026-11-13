using Reki.Application.Posts.GetWorldPosts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public interface IPostRepository
{
    ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<PostSimilarity>> FindSimilarPostsAsync(
        GamePost post,
        double threshold);

    /// <summary>
    /// Reads posts in a world whose ownership the application has already verified.
    /// Returns posts ordered by descending UUID and the cursor for the next page, if any.
    /// </summary>
    ValueTask<WorldPostsPage> GetPostsAsync(
        WorldId worldId,
        IReadOnlyCollection<AuthorId> authorIds,
        PostId? cursor,
        int pageSize,
        AuthorId? likedBy,
        CancellationToken cancellationToken);

    ValueTask<GamePost?> GetPostAsync(
        PostId postId,
        CancellationToken cancellationToken);

    ValueTask LikePostAsync(PostId postId, AuthorId authorId, CancellationToken cancellationToken);

    ValueTask SaveReplyAsync(GamePost post, CancellationToken cancellationToken);

    ValueTask DeletePostsAsync(IReadOnlyCollection<WorldId> worldIds, CancellationToken cancellationToken);

    ValueTask DeletePostAsync(PostId postId, CancellationToken cancellationToken);
}
