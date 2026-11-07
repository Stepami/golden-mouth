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
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GenerateReplyCommand;

internal sealed partial class GenerateReplyCommandHandler(
    ISender sender,
    IRekiContextAccessor rekiContextAccessor,
    StarTurnPolicy turnPolicy,
    IStarTurnGenerator generator,
    ILogger<GenerateReplyCommandHandler> logger) : IStreamCommandHandler<GenerateReply, ReplyGenerationLiveEvent>
{
    private const double AdditionalStarProbability = 0.5;

    public async IAsyncEnumerable<ReplyGenerationLiveEvent> Handle(
        GenerateReply request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(request.PostId, world.Id), cancellationToken);
        var star = world.FindStar(request.AuthorId)
            ?? throw new RequestConflictException("The author is not a star of this world.");
        if (request.SourceCommentId is null)
        {
            if (request.Additional || post.AuthorId != player.Id.ToAuthorId() || post.PredictedAuthorId != request.AuthorId)
            {
                throw new RequestConflictException("The author does not match the post.");
            }

            var postContext = new StarReplyContext(new StarAgentProfile(world, star.Id, player.Address), star.Id, post.Text, [], post.Text, turnPolicy.IsHateEnabled(player, world));
            await foreach (var item in GenerateTextAsync(postContext, request.AuthorId, null, cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        var source = post.Comments.FirstOrDefault(item =>
                item.Id == request.SourceCommentId && !item.Hidden && item.AuthorId == player.Id.ToAuthorId())
            ?? throw new ResourceNotFoundException("Player comment not found.");
        var primary = source.ParentCommentId is { } parentId
            ? world.FindStar(post.Comments.First(item => item.Id == parentId).AuthorId)
            : world.FindStar(post.AuthorId);
        if (primary is null)
        {
            throw new RequestConflictException("The thread star is no longer in this world.");
        }

        var others = world.Stars.Where(item => item.Id != primary.Id).ToArray();
        var bytes = source.Id.Value.ToByteArray();
        StarId? additionalStarId = others.Length > 0 &&
                                   BitConverter.ToUInt32(bytes, 0) / 4294967296d < AdditionalStarProbability
            ? others[(int)(BitConverter.ToUInt32(bytes, 4) % (uint)others.Length)].Id
            : null;
        if (request.Additional ? additionalStarId != star.Id : primary.Id != star.Id)
        {
            throw new RequestConflictException("The star does not belong to the selected thread.");
        }

        var branch = source.ParentCommentId is { } parent ? post.GetBranch(parent) : [];
        var turnContext = new StarReplyContext(
            new StarAgentProfile(world, star.Id, player.Address),
            star.Id,
            post.Text,
            branch,
            source.Text,
            turnPolicy.IsHateEnabled(player, world),
            request.Additional);
        await foreach (var item in GenerateTextAsync(turnContext, request.AuthorId, request.Additional ? null : additionalStarId, cancellationToken))
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<ReplyGenerationLiveEvent> GenerateTextAsync(
        StarReplyContext turnContext,
        AuthorId authorId,
        StarId? additionalStarId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Generating, authorId, null, null, null);
        ReplyGenerationLiveEvent outcome;
        try
        {
            var text = new StringBuilder();
            var hasToolCall = false;
            await foreach (var contents in generator.StreamReplyTurnAsync(turnContext, cancellationToken).Select(update => update.Contents))
            {
                text.Append(string.Concat(contents.OfType<TextContent>().Select(chunk => chunk.Text)));
                hasToolCall |= contents.Any(content => content is FunctionCallContent);
            }

            var reply = text.ToString().Trim();
            if (hasToolCall || reply.Length is 0 or > 500)
            {
                throw new AiServiceUnavailableException("The model returned an invalid thread reply.");
            }

            PlayerSafetyGuard.EnsureOutputAllowed([reply]);
            outcome = new ReplyGenerationLiveEvent(
                ReplyGenerationStatus.Completed,
                authorId,
                reply,
                additionalStarId,
                null);
        }
        catch (UnsafeGeneratedContentException)
        {
            outcome = new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, authorId, null, null, "unsafe_generated_content");
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException) &&
                                          (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
        {
            GenerationFailed();
            outcome = new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, authorId, null, null, "ai_unavailable");
        }

        yield return outcome;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Thread generation failed")]
    private partial void GenerationFailed();
}
