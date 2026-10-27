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

        GamePost? source = null;
        GameComment? comment = null;
        IReadOnlyList<GameComment> branch = [];
        if (request.SourceEvent != StarFeedEvent.FeedOpened)
        {
            source = request.SourcePostId is { } sourceId
                ? await postRepository.GetPostAsync(sourceId, worldId, cancellationToken)
                : null;
            if (source is null)
            {
                throw new TerminateExecutionException("The feed source is unavailable.");
            }

            if (request.SourceEvent == StarFeedEvent.PostPublished)
            {
                if (source.AuthorId != playerId.ToAuthorId() || request.SourceCommentId is not null)
                {
                    throw new TerminateExecutionException("The feed source is invalid.");
                }
            }
            else if (request.SourceEvent == StarFeedEvent.CommentPublished)
            {
                comment = source.Comments.FirstOrDefault(item => item.Id == request.SourceCommentId);
                if (comment is null || comment.Hidden || comment.AuthorId != playerId.ToAuthorId())
                {
                    throw new TerminateExecutionException("The feed comment is unavailable.");
                }

                branch = source.GetBranch(comment.Id);
                if (branch.Any(item => item.Hidden))
                {
                    throw new TerminateExecutionException("The feed branch is unavailable.");
                }

                branch = branch.TakeLast(8).ToArray();
            }
            else
            {
                throw new TerminateExecutionException("The feed event is invalid.");
            }
        }

        if (request.SourceEvent == StarFeedEvent.FeedOpened && (request.SourcePostId is not null || request.SourceCommentId is not null))
        {
            throw new TerminateExecutionException("The feed event has an unexpected source.");
        }

        var star = world.Stars[(int)((uint)context.Id.GetHashCode() % (uint)world.Stars.Count)];
        var own = await postRepository.GetPostsAsync(worldId, star.Id.ToAuthorId(), null, 20, cancellationToken);
        var recent = await postRepository.GetPostsAsync(worldId, null, null, 10, cancellationToken);
        var profile = new StarAgentProfile(world, star.Id, player.Address);
        var existingReplies = source?.Comments
            .Where(item => !item.Hidden && (comment is null || item.ParentCommentId == comment.Id))
            .TakeLast(8).ToArray() ?? [];
        var feedContext = new StarFeedContext(profile, request.SourceEvent, own.Items, recent.Items, source, comment, branch, existingReplies);
        var text = await generator.GenerateFeedPostAsync(feedContext, cancellationToken);
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

        if (source is not null)
        {
            var freshSource = await postRepository.GetPostAsync(source.Id, worldId, cancellationToken);
            if (freshSource is null || (comment is not null &&
                !freshSource.Comments.Any(item => item.Id == comment.Id && !item.Hidden && item.AuthorId == playerId.ToAuthorId())))
            {
                throw new TerminateExecutionException("The feed source is unavailable.");
            }
        }

        var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(request.CreatedAtUnixMilliseconds);
        var post = new GamePost(postId, star.Id.ToAuthorId(), worldId, text, 0, [], createdAt, 0, 0, 1, false, false);
        await postRepository.InsertPostAsync(post, cancellationToken);
    }

    private static string NormalizeText(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
