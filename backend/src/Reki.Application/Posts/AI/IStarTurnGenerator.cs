using Microsoft.Extensions.AI;
using Reki.Application.Posts.GenerateReplyCommand;

namespace Reki.Application.Posts.AI;

public interface IStarTurnGenerator
{
    IAsyncEnumerable<ChatResponseUpdate> StreamReplyTurnAsync(
        StarReplyContext context,
        CancellationToken cancellationToken);
}
