using Reki.Application.Posts.AI.Comment;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Posts.AI.Post;

namespace Reki.Application.Posts.AI;

public interface IStarContentGenerator
{
    /// <summary>Returns the generated feed text, or null when it fails content validation.</summary>
    ValueTask<string?> GenerateFeedPostAsync(
        StarFeedContext context,
        CancellationToken cancellationToken);

    ValueTask<string?> GeneratePostReactionAsync(
        StarFeedPostContext context,
        CancellationToken cancellationToken);

    ValueTask<string?> GenerateCommentReactionAsync(
        StarFeedCommentContext context,
        CancellationToken cancellationToken);
}
