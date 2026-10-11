using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;

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
        var player = await store.GetPlayerAsync(request.PlayerId, cancellationToken)
                     ?? throw new ResourceNotFoundException("Player not found.");
        var post = await sender.Send(new GetPostQuery(request.PostId, player.Id), cancellationToken);

        return StoryCardData.From(player.EnsureDefaults(), post);
    }
}
