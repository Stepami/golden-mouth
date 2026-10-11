using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;

namespace Reki.Application.Posts;

public sealed class GetPublicStoryCardHandler(IGameStore store)
    : IRequestHandler<GetPublicStoryCardQuery, StoryCardData>
{
    public async ValueTask<StoryCardData> Handle(
        GetPublicStoryCardQuery request,
        CancellationToken cancellationToken)
    {
        var player = await store.GetPlayerAsync(request.PlayerId, cancellationToken)
                     ?? throw new ResourceNotFoundException("Player not found.");
        var post = await store.GetPostAsync(player.Id, request.PostId, cancellationToken)
                   ?? throw new ResourceNotFoundException("Post not found.");

        return StoryCardData.From(player.EnsureDefaults(), post);
    }
}
