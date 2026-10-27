using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Posts.AI;
using Reki.Application.Worlds;
using Reki.Domain.Posts;
using Reki.Infrastructure.Jobs.StarFeed;
using TickerQ.Exceptions;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs.GenerateStarFeedOnFeedOpened;

internal sealed class GenerateStarFeedOnFeedOpenedJob(
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
        var postId = request.PostId;
        var player = await playerRepository.GetPlayerAsync(playerId, cancellationToken);
        var world = await worldRepository.GetWorldAsync(playerId, worldId, cancellationToken);
        if (player is null || world is null || world.Stars.Count == 0)
        {
            throw new TerminateExecutionException("The feed world is unavailable.");
        }

        if (await postRepository.GetPostAsync(postId, worldId, cancellationToken) is not null)
        {
            return;
        }

        var star = world.Stars[(int)((uint)context.Id.GetHashCode() % (uint)world.Stars.Count)];
        var own = await postRepository.GetPostsAsync(worldId, star.Id.ToAuthorId(), null, 20, cancellationToken);
        var recent = await postRepository.GetPostsAsync(worldId, null, null, 10, cancellationToken);
        var profile = new StarAgentProfile(world, star.Id, player.Address);
        var feed = new StarFeedContext(profile, own.Items, recent.Items);
        var text = await generator.GenerateFeedPostAsync(feed, cancellationToken);
        if (text is null)
        {
            throw new TerminateExecutionException("The generated feed content is invalid.");
        }

        var normalized = NormalizeText(text);
        if (own.Items.Any(post => string.Equals(NormalizeText(post.Text), normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        // Recheck ownership after the external call, including deletion while generating.
        var freshWorld = await worldRepository.GetWorldAsync(playerId, worldId, cancellationToken);
        if (freshWorld?.FindStar(star.Id) is null ||
            await playerRepository.GetPlayerAsync(playerId, cancellationToken) is null)
        {
            throw new TerminateExecutionException("The feed world is unavailable.");
        }

        var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(request.CreatedAtUnixMilliseconds);
        var post = new GamePost(postId, star.Id.ToAuthorId(), worldId, text, 0, [], createdAt, 0, 0, 1, false, false);
        await postRepository.InsertPostAsync(post, cancellationToken);
    }

    private static string NormalizeText(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
