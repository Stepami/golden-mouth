using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Player;
using Reki.Application.Worlds;

namespace Reki.Application.Posts.GetPublicStoryCard;

internal sealed class PublicStoryCardQueryHandler(
    IPlayerRepository playerRepository,
    IPostRepository postRepository,
    IWorldRepository worldRepository)
    : IQueryHandler<PublicStoryCardQuery, StoryCardData>
{
    public async ValueTask<StoryCardData> Handle(
        PublicStoryCardQuery query,
        CancellationToken cancellationToken)
    {
        var player = await playerRepository.GetPlayerAsync(query.PlayerId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
        var post = await postRepository.GetPostAsync(query.PostId, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        var world = await worldRepository.GetWorldAsync(player.Id, post.WorldId, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");

        return StoryCardData.From(world, post);
    }
}
