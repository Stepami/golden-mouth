using Mediator;
using Microsoft.Extensions.Logging;
using Reki.Application.RekiContext;

namespace Reki.Application.Player.Challenges;

internal sealed partial class DailyProgressBehavior<TCommand, TResponse>(
    IStateComparer<TCommand, TResponse> comparer,
    IRekiContextAccessor contextAccessor,
    DailyProgress progress,
    ILogger<DailyProgressBehavior<TCommand, TResponse>> logger) : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ITrackDailyProgress
{
#pragma warning disable AV1755 // Mediator defines the asynchronous entry point as Handle.
    public async ValueTask<TResponse> Handle(
        TCommand message,
        MessageHandlerDelegate<TCommand, TResponse> next,
        CancellationToken cancellationToken)
#pragma warning restore AV1755
    {
        Func<TResponse, CancellationToken, ValueTask>? apply = null;
        try
        {
            var context = contextAccessor.Context;
            var challenge = (await context.Player).DailyChallenge;
            if (challenge.Day == progress.Day)
            {
                apply = await comparer.CaptureAsync(message, context, challenge, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException))
        {
            ProgressTrackingFailed(typeof(TCommand).Name, "capture", exception.GetType().Name);
        }

        var response = await next(message, cancellationToken);

        if (apply is not null)
        {
            try
            {
                await apply(response, cancellationToken);
            }
            catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException))
            {
                ProgressTrackingFailed(typeof(TCommand).Name, "apply", exception.GetType().Name);
            }
        }

        return response;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Daily challenge progress tracking failed for {CommandType} during {Stage} ({ExceptionType})")]
    private partial void ProgressTrackingFailed(string commandType, string stage, string exceptionType);
}
