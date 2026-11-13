using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Comment;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Worlds;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using TickerQ.Exceptions;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs.GenerateStarFeed.OnCommentPublished;

internal sealed class GenerateStarFeedOnCommentPublishedJob(
    IPlayerRepository playerRepository,
    IPostRepository postRepository,
    IWorldRepository worldRepository,
    IStarContentGenerator generator) : ITickerFunction<StarFeedOnCommentPublishedRequest>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<StarFeedOnCommentPublishedRequest> context,
        CancellationToken cancellationToken = default)
    {
        var request = context.Request;
        var source = await postRepository.GetPostAsync(request.SourcePostId, request.WorldId, cancellationToken)
            ?? throw new TerminateExecutionException("The feed source is unavailable.");
        var comment = source.Comments.FirstOrDefault(item => item.Id == request.SourceCommentId);
        if (comment is null || comment.Hidden || comment.AuthorId != request.PlayerId.ToAuthorId())
        {
            throw new TerminateExecutionException("The feed comment is unavailable.");
        }

        var branch = source.GetBranch(comment.Id);
        if (branch.Any(item => item.Hidden))
        {
            throw new TerminateExecutionException("The feed branch is unavailable.");
        }

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
        var own = await postRepository.GetPostsAsync(worldId, star.Id.ToAuthorId(), null, 20, null, cancellationToken);
        var recent = await postRepository.GetPostsAsync(worldId, null, null, 10, null, cancellationToken);
        var profile = new StarAgentProfile(world, star.Id, player.Address);
        var replies = source.Comments.Where(item => !item.Hidden && item.ParentCommentId == comment.Id).TakeLast(8).ToArray();
        var feed = new StarFeedContext(profile, own.Items, recent.Items, StarFeedEventType.CommentPublished);
        var text = await generator.GenerateContentAsync(
            new StarFeedCommentContext(feed, source, comment, [.. branch.TakeLast(8)], replies), cancellationToken);
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

        var freshSource = await postRepository.GetPostAsync(source.Id, worldId, cancellationToken);
        if (freshSource is null ||
            !freshSource.Comments.Any(item => item.Id == comment.Id && !item.Hidden && item.AuthorId == playerId.ToAuthorId()))
        {
            throw new TerminateExecutionException("The feed source is unavailable.");
        }

        var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(request.CreatedAtUnixMilliseconds);
        var post = new GamePost(postId, star.Id.ToAuthorId(), worldId, text, 0, 0, [], [], createdAt, 0, 0, 1, false, false);
        await postRepository.InsertPostAsync(post, cancellationToken);
    }
}
