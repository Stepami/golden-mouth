using Microsoft.Extensions.AI;
using Reki.Application.Posts.GeneratePublicationCommand;
using Reki.Application.Posts.GenerateReplyCommand;

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
