using Microsoft.Extensions.AI;
using Reki.Application.Posts.AddReplyCommand;
using Reki.Application.Posts.GeneratePublicationCommand;

namespace Reki.Application.Posts.AI;

public interface IStarTurnGenerator
{
    IAsyncEnumerable<ChatResponseUpdate> StreamPostTurnAsync(
        StarPostContext context,
        CancellationToken cancellationToken);

    IAsyncEnumerable<ChatResponseUpdate> StreamReplyTurnAsync(
        StarReplyContext context,
        CancellationToken cancellationToken);
}
