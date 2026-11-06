using System.Runtime.CompilerServices;
using Mediator;
using Microsoft.Extensions.AI;
using Reki.Application.Exceptions;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.GeneratePublicationCommand;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Application.Posts.Responses.Reply.Generation;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GenerateReplyCommand;

internal sealed class GeneratePostReply(
    ISender sender,
    IRekiContextAccessor rekiContextAccessor,
    StarTurnPolicy turnPolicy,
    PlayerSafetyGuard safety,
    IStarTurnGenerator generator,
    ReplyTextGenerator replyText,
    IPostRepository posts,
    PostPublisher publisher,
    ProductTelemetry telemetry)
{
    public async IAsyncEnumerable<ReplyGenerationLiveEvent> GenerateAsync(
        GamePost post,
        PlayerProfile player,
        World world,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Generating, null, null, null, null);
        if (post.EvaluationApplied)
        {
            if (post.PredictedAuthorId is not { } author || world.FindStar(author) is not { } star)
            {
                yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, null, null, null, "ai_unavailable");
                yield break;
            }

            var existing = post.Comments.FirstOrDefault(comment => comment.AuthorId == author && comment.ParentCommentId is null);
            if (existing is not null)
            {
                yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Completed, author, existing.Text, null, null);
                yield break;
            }

            var context = new StarReplyContext(new StarAgentProfile(world, star.Id, player.Address), star.Id, post.Text, [], post.Text, turnPolicy.IsHateEnabled(player, world), false);
            await foreach (var item in replyText.GenerateAsync(context, author, null, cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        var recent = await sender.Send(new WorldPostsQuery(world.Id, null, 3), cancellationToken);
        var stage = world.Player.Subscribers switch { < 1000 => "mask", < 5000 => "views", < 10000 => "strike", _ => "new_line" };
        var postContext = new StarPostContext(post.Text, new StarAgentProfile(world, null, player.Address), stage, recent.Items.Where(item => item.Id != post.Id).Select(item => item.Text).ToArray(), turnPolicy.IsHateEnabled(player, world));
        GeneratedPostTurn? turn = null;
        var code = "ai_unavailable";
        try
        {
            var actions = new List<StarToolCallParser.IStarAction>();
            await foreach (var update in generator.StreamPostTurnAsync(postContext, cancellationToken))
            {
                actions.AddRange(StarToolCallParser.Parse(update.Contents.OfType<FunctionCallContent>()));
            }

            var comment = actions.OfType<StarToolCallParser.StarCommentAction>().Single();
            var reactions = actions.OfType<StarToolCallParser.StarReactionAction>().ToArray();
            if (reactions.Length > 1)
            {
                throw new AiServiceUnavailableException("Invalid actions.");
            }

            var star = world.FindStar(comment.StarId) ?? throw new AiServiceUnavailableException("Invalid star.");
            turn = new GeneratedPostTurn(star.Name, star.Id, comment.Text, comment.Hook, reactions.FirstOrDefault()?.Emoji ?? "🔥", comment.Brave, comment.Humor, comment.Topic, comment.Reasons, comment.OnTopic, comment.Tone);
            turnPolicy.EnsurePostAllowed(turn, player, world);
            PlayerSafetyGuard.EnsureOutputAllowed([turn.Comment, turn.Hook]);
        }
        catch (UnsafeGeneratedContentException)
        {
            code = "unsafe_generated_content";
            turn = null;
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException) &&
                                          (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
        {
            turn = null;
        }

        if (turn is null)
        {
            telemetry.TrackPlayer(player, "post_failed", new Dictionary<string, string> { ["id"] = post.Id.ToString() });
            yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, null, null, null, code);
            yield break;
        }

        rekiContextAccessor.Context.Reload();
        player = await rekiContextAccessor.Context.Player;
        world = (await rekiContextAccessor.Context.Worlds).GetValueOrDefault(world.Id)
            ?? throw new ResourceNotFoundException("World not found.");
        post = await posts.GetPostAsync(post.Id, world.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        if (post.EvaluationApplied)
        {
            await foreach (var item in GenerateAsync(post, player, world, cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        turnPolicy.EnsurePostAllowed(turn, player, world);
        await safety.EnsureInputAllowedAsync(player, [post.Text], cancellationToken);

        var published = await publisher.PublishAsync(post, player, world, turn, null, cancellationToken);
        if (published is null)
        {
            var saved = await posts.GetPostAsync(post.Id, world.Id, cancellationToken);
            if (saved is { EvaluationApplied: true })
            {
                rekiContextAccessor.Context.Reload();
                player = await rekiContextAccessor.Context.Player;
                world = (await rekiContextAccessor.Context.Worlds).GetValueOrDefault(world.Id)
                    ?? throw new ResourceNotFoundException("World not found.");
                await foreach (var item in GenerateAsync(saved, player, world, cancellationToken))
                {
                    yield return item;
                }

                yield break;
            }

            yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, null, null, null, "ai_unavailable");
            yield break;
        }

        yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Completed, turn.StarId.ToAuthorId(), turn.Comment, null, null);
    }
}
