using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Player;

namespace Reki.Application.Posts;

public sealed class GetPublicStoryCardHandler(
    ISender sender,
    IGameStore store)
    : IRequestHandler<GetPublicStoryCardQuery, StoryCardData>
{
    public async ValueTask<StoryCardData> Handle(
        GetPublicStoryCardQuery request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(request.PlayerId), cancellationToken);
        var post = await store.GetPostAsync(request.PostId, player.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        var world = await sender.Send(new Worlds.GetWorldQuery(player.Id, post.WorldId), cancellationToken);

        return StoryCardData.From(world, post);
    }
}