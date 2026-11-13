using System.Runtime.CompilerServices;
using System.Text;
using Mediator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Reki.Application.Exceptions;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.GetPost;
using Reki.Application.Posts.Responses.Reply.Generation;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.GenerateReplyCommand;

internal sealed partial class GenerateReplyCommandHandler(
    ISender sender,
    IPostRepository posts,
    IRekiContextAccessor rekiContextAccessor,
    StarTurnPolicy turnPolicy,
    IStarTurnGenerator generator,
    ILogger<GenerateReplyCommandHandler> logger) : IStreamCommandHandler<GenerateReply, ReplyGenerationLiveEvent>
{
    public async IAsyncEnumerable<ReplyGenerationLiveEvent> Handle(
        GenerateReply command,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(command.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(command.PostId, world.Id), cancellationToken);
        if (post.IsRepost)
        {
            throw new RequestConflictException("Open the original post to reply.");
        }

        var star = world.FindStar(command.AuthorId)
            ?? throw new RequestConflictException("The author is not a star of this world.");
        var source = command.SourceCommentId is { } sourceId
            ? post.Comments.FirstOrDefault(item => item.Id == sourceId && !item.Hidden && item.AuthorId == player.Id.ToAuthorId())
                ?? throw new ResourceNotFoundException("Player comment not found.")
            : null;
        if (source is not null)
        {
            var primary = source.ParentCommentId is { } parentId
                ? world.FindStar(post.Comments.First(item => item.Id == parentId).AuthorId)
                : world.FindStar(post.AuthorId);
            if (primary is null || (command.Additional ? source.AdditionalReplierId != star.Id : primary.Id != star.Id))
            {
                throw new RequestConflictException("The star does not belong to the selected thread.");
            }
        }

        var additionalStarId = source?.AdditionalReplierId;
        var branch = source?.ParentCommentId is { } parent ? post.GetBranch(parent) : [];
        var quote = post.QuotedPostId is { } quoteId ? await posts.GetPostAsync(quoteId, world.Id, cancellationToken) : null;
        var turnContext = new StarReplyContext(
            new StarAgentProfile(world, star.Id, player.Address),
            star.Id,
            quote is null ? post.Text : $"Цитата из поста {world.FindStar(quote.AuthorId)?.Name}: {quote.Text}\nТекст игрока: {post.Text}",
            branch,
            source?.Text,
            turnPolicy.IsHateEnabled(player, world),
            command.Additional);
        yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Generating, command.AuthorId, null, null, null);
        var text = new StringBuilder();
        var hasToolCall = false;
        await using var updates = generator.StreamReplyTurnAsync(turnContext, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool hasUpdate;
            var failed = false;
            try
            {
                hasUpdate = await updates.MoveNextAsync();
            }
            catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException) &&
                                              (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
            {
                GenerationFailed();
                failed = true;
                hasUpdate = false;
            }

            if (failed)
            {
                yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, command.AuthorId, null, null, "ai_unavailable");
                yield break;
            }

            if (!hasUpdate)
            {
                break;
            }

            text.Append(string.Concat(updates.Current.Contents.OfType<TextContent>().Select(chunk => chunk.Text)));
            hasToolCall |= updates.Current.Contents.Any(content => content is FunctionCallContent);

            yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Generating, command.AuthorId, null, null, null);
        }

        ReplyGenerationLiveEvent outcome;
        try
        {
            var reply = text.ToString().Trim();
            if (hasToolCall || reply.Length is 0 or > 500)
            {
                throw new AiServiceUnavailableException("The model returned an invalid thread reply.");
            }

            PlayerSafetyGuard.EnsureOutputAllowed([reply]);
            outcome = new ReplyGenerationLiveEvent(
                ReplyGenerationStatus.Completed,
                command.AuthorId,
                reply,
                command.Additional ? null : additionalStarId,
                null);
        }
        catch (UnsafeGeneratedContentException)
        {
            outcome = new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, command.AuthorId, null, null, "unsafe_generated_content");
        }
        catch (AiServiceUnavailableException)
        {
            GenerationFailed();
            outcome = new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, command.AuthorId, null, null, "ai_unavailable");
        }

        yield return outcome;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Thread generation failed")]
    private partial void GenerationFailed();
}
