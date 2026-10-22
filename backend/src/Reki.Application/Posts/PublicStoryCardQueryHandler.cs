using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Application.Player;
using Reki.Application.Worlds;

namespace Reki.Application.Posts;

public sealed class PublicStoryCardQueryHandler(
    IPlayerRepository playerRepository,
    IPostRepository postRepository,
    IWorldRepository worldRepository)
    : IQueryHandler<PublicStoryCardQuery, StoryCardData>
{
    public async ValueTask<StoryCardData> Handle(
        PublicStoryCardQuery request,
        CancellationToken cancellationToken)
    {
        var player = await playerRepository.GetPlayerAsync(request.PlayerId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
        var post = await postRepository.GetPublicStoryPostAsync(request.PostId, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        var world = await worldRepository.GetWorldAsync(player.Id, post.WorldId, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");

        return StoryCardData.From(world, post);
    }
}
