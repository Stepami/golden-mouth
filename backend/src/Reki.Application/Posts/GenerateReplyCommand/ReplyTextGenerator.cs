using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Reki.Application.Exceptions;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.Responses.Reply.Generation;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GenerateReplyCommand;

internal sealed partial class ReplyTextGenerator(IStarTurnGenerator generator, ILogger<ReplyTextGenerator> logger)
{
    public async IAsyncEnumerable<ReplyGenerationLiveEvent> GenerateAsync(
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
