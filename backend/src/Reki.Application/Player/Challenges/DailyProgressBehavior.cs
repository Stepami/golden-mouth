using Mediator;
using Microsoft.Extensions.Logging;
using Reki.Application.RekiContext;
using Reki.Domain.Player;

namespace Reki.Application.Player.Challenges;

internal sealed partial class DailyProgressBehavior<TCommand, TResponse>(
    IStateComparer<TCommand, TResponse> comparer,
    IRekiContextAccessor contextAccessor,
    IPlayerRepository players,
    TimeProvider timeProvider,
    ILogger<DailyProgressBehavior<TCommand, TResponse>> logger) : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ITrackDailyProgress
{
    public async ValueTask<TResponse> Handle(
        TCommand message,
        MessageHandlerDelegate<TCommand, TResponse> next,
        CancellationToken cancellationToken)
    {
        PlayerProfile? player = null;
        DateOnly day = default;
        Func<TResponse, CancellationToken, ValueTask> apply = static (_, _) => ValueTask.CompletedTask;
        try
        {
            var candidate = await contextAccessor.Context.Player;
            day = candidate.DailyChallenge.Day;
            if (day == GetUtcDay())
            {
                apply = await comparer.CaptureAsync(message, candidate, cancellationToken);
                player = candidate;
            }
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException))
        {
            ProgressTrackingFailed(typeof(TCommand).Name, "capture", exception.GetType().Name);
        }

        var response = await next(message, cancellationToken);

        if (player is not null)
        {
            try
            {
                if (day != GetUtcDay() || day != player.DailyChallenge.Day)
                {
                    return response;
                }

                var previousVersion = player.Version;
                await apply(response, cancellationToken);
                if (day == GetUtcDay() && day == player.DailyChallenge.Day)
                {
                    await players.SaveDailyChallengeAsync(player, previousVersion, cancellationToken);
                }
            }
            catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException))
            {
                ProgressTrackingFailed(typeof(TCommand).Name, "apply", exception.GetType().Name);
            }
        }

        return response;
    }

    private DateOnly GetUtcDay() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Daily challenge progress tracking failed for {CommandType} during {Stage} ({ExceptionType})")]
    private partial void ProgressTrackingFailed(string commandType, string stage, string exceptionType);
}
