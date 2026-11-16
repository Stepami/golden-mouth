using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Worlds;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using TickerQ.Exceptions;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs.GenerateStarFeed.OnPostPublished;

internal sealed class GenerateStarFeedOnPostPublishedJob(
    IPlayerRepository playerRepository,
    IPostRepository postRepository,
    IWorldRepository worldRepository,
    IStarContentGenerator generator) : ITickerFunction<StarFeedOnPostPublishedRequest>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<StarFeedOnPostPublishedRequest> context,
        CancellationToken cancellationToken = default)
    {
        var (playerId, worldId, postId, createdAtUnixMilliseconds, sourcePostId) = context.Request;
        var source = await postRepository.GetPostAsync(sourcePostId, cancellationToken)
            ?? throw new TerminateExecutionException("The feed source is unavailable.");
        if (source.AuthorId != playerId.ToAuthorId())
        {
            throw new TerminateExecutionException("The feed source is invalid.");
        }

        var player = await playerRepository.GetPlayerAsync(playerId, cancellationToken);
        var world = await worldRepository.GetWorldAsync(playerId, worldId, cancellationToken);
        if (player is null || world is null || world.Stars.Count == 0)
        {
            throw new TerminateExecutionException("The feed world is unavailable.");
        }

        if (await postRepository.GetPostAsync(postId, cancellationToken) is not null)
        {
            return;
        }

        var star = world.Stars[(int)((uint)context.Id.GetHashCode() % (uint)world.Stars.Count)];
        var own = await postRepository.GetPostsAsync(worldId, [star.Id.ToAuthorId()], null, 20, null, cancellationToken);
        var recent = await postRepository.GetPostsAsync(worldId, [], null, 10, null, cancellationToken);
        var profile = new StarAgentProfile(world, star.Id, player.Address);
        var replies = source.Comments.Where(item => !item.Hidden).TakeLast(8).ToArray();
        var feed = new StarFeedContext(profile, own.Items, recent.Items, StarFeedEventType.PostPublished);
        var text = await generator.GenerateContentAsync(new StarFeedPostContext(feed, source, replies), cancellationToken);
        if (text is null)
        {
            throw new TerminateExecutionException("The generated feed content is invalid.");
        }

        // Recheck ownership after the external call, including deletion while generating.
        var freshWorld = await worldRepository.GetWorldAsync(playerId, worldId, cancellationToken);
        if (freshWorld?.FindStar(star.Id.ToAuthorId()) is null ||
            await playerRepository.GetPlayerAsync(playerId, cancellationToken) is null)
        {
            throw new TerminateExecutionException("The feed world is unavailable.");
        }

        var freshSource = await postRepository.GetPostAsync(source.Id, cancellationToken);
        if (freshSource is null || freshSource.AuthorId != playerId.ToAuthorId())
        {
            throw new TerminateExecutionException("The feed source is unavailable.");
        }

        var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(createdAtUnixMilliseconds);
        var post = new GamePost(postId, star.Id.ToAuthorId(), worldId, playerId, text, 0, 0, [], [], createdAt, 0, 0, 1, false, false);
        await postRepository.InsertPostAsync(post, cancellationToken);
    }
}
