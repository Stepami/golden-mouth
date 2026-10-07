using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetStoryCardHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetStoryCardQuery, StoryCardData>
{
    public async ValueTask<StoryCardData> Handle(
        GetStoryCardQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(
            request.TelegramUserId,
            cancellationToken);
        GamePost post = await store.GetPostAsync(
            player.Id,
            request.PostId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");

        return StoryCardData.From(player, post);
    }
}
