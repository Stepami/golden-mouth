using Mediator;
using Reki.Application.Player;

namespace Reki.Application.Posts;

public sealed class GetPublicStoryCardHandler(
    ISender sender)
    : IRequestHandler<GetPublicStoryCardQuery, StoryCardData>
{
    public async ValueTask<StoryCardData> Handle(
        GetPublicStoryCardQuery request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(request.PlayerId), cancellationToken);
        var post = await sender.Send(new GetPostQuery(request.PostId, player.Id), cancellationToken);

        return StoryCardData.From(player, post);
    }
}
