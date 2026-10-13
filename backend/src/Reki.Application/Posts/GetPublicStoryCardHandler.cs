using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;

namespace Reki.Application.Posts;

public sealed class GetPublicStoryCardHandler(
    IGameStore store)
    : IRequestHandler<GetPublicStoryCardQuery, StoryCardData>
{
    public async ValueTask<StoryCardData> Handle(
        GetPublicStoryCardQuery request,
        CancellationToken cancellationToken)
    {
        var player = await store.GetPlayerAsync(request.PlayerId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
        var post = await store.GetPublicStoryPostAsync(request.PostId, player.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        var world = await store.GetWorldAsync(player.Id, post.WorldId, cancellationToken)
            ?? throw new ResourceNotFoundException("World not found.");

        return StoryCardData.From(world, post);
    }
}