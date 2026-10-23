using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Posts.AI;
using Reki.Application.Worlds;
using Reki.Domain.Posts;
using TickerQ.Exceptions;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs.GenerateStarFeedPost;

internal sealed class GenerateStarFeedPostJob(
    IPlayerRepository playerRepository,
    IPostRepository postRepository,
    IWorldRepository worldRepository,
    IStarTurnGenerator generator) : ITickerFunction<StarFeedRequest>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<StarFeedRequest> context,
        CancellationToken cancellationToken = default)
    {
        var request = context.Request;
        var playerId = request.PlayerId;
        var worldId = request.WorldId;
        var player = await playerRepository.GetPlayerAsync(playerId, cancellationToken);
        var world = await worldRepository.GetWorldAsync(playerId, worldId, cancellationToken);
        if (player is null || world is null || world.Stars.Count == 0)
        {
            throw new TerminateExecutionException("The feed world is unavailable.");
        }

        var postId = request.PostId;
        if (await postRepository.GetPostAsync(postId, worldId, cancellationToken) is not null)
        {
            return;
        }

        var recent = await postRepository.GetPostsAsync(worldId, null, null, 20, cancellationToken);
        var star = world.Stars[(int)((uint)context.Id.GetHashCode() % (uint)world.Stars.Count)];
        var profile = new StarAgentProfile(world, star.Id, player.Address);
        var text = await generator.GenerateFeedPostAsync(profile, recent.Items, cancellationToken);
        if (text is null)
        {
            throw new TerminateExecutionException("The generated feed content is invalid.");
        }

        // Recheck ownership after the external call, including deletion while generating.
        var freshWorld = await worldRepository.GetWorldAsync(playerId, worldId, cancellationToken);
        if (freshWorld?.FindStar(star.Id) is null ||
            await playerRepository.GetPlayerAsync(playerId, cancellationToken) is null)
        {
            throw new TerminateExecutionException("The feed world is unavailable.");
        }

        var post = new GamePost(postId, star.Id.ToAuthorId(), worldId, text, 0, [], postId.GetTimestamp(), 0, 0, 1, false, false);
        await postRepository.InsertPostAsync(post, cancellationToken);
    }
}
