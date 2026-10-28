using Microsoft.Extensions.AI;
using Reki.Application.Posts.AddReplyCommand;
using Reki.Application.Posts.AI.Comment;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Posts.GeneratePublicationCommand;

namespace Reki.Application.Posts.AI;

public interface IStarTurnGenerator
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

    IAsyncEnumerable<ChatResponseUpdate> StreamPostTurnAsync(
        StarPostContext context,
        CancellationToken cancellationToken);

    IAsyncEnumerable<ChatResponseUpdate> StreamReplyTurnAsync(
        StarReplyContext context,
        CancellationToken cancellationToken);
}
