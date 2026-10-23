using Microsoft.Extensions.AI;
using Reki.Application.Posts.AddReplyCommand;
using Reki.Application.Posts.GeneratePublicationCommand;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI;

public interface IStarTurnGenerator
{
    /// <summary>Returns the generated feed text, or null when it fails content validation.</summary>
    ValueTask<string?> GenerateFeedPostAsync(
        StarAgentProfile profile,
        IReadOnlyList<GamePost> recentPosts,
        CancellationToken cancellationToken);

    IAsyncEnumerable<ChatResponseUpdate> StreamPostTurnAsync(
        StarPostContext context,
        CancellationToken cancellationToken);

    IAsyncEnumerable<ChatResponseUpdate> StreamReplyTurnAsync(
        StarReplyContext context,
        CancellationToken cancellationToken);
}
