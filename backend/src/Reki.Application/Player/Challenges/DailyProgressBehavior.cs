using Mediator;
using Microsoft.Extensions.Logging;
using Reki.Application.RekiContext;

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
        var player = await contextAccessor.Context.Player;
        var day = player.DailyChallenge.Day;
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (day != today || player.DailyChallenge.IsCompleted)
        {
            return await next(message, cancellationToken);
        }

        var continuation = EmptyStateComparerContinuation<TCommand, TResponse>.Instance;
        try
        {
            continuation = await comparer.CaptureAsync(message, player, cancellationToken);
        }
        catch (Exception exception)
        {
            ProgressTrackingFailed(exception, typeof(TCommand).Name, "capture");
        }

        var response = await next(message, cancellationToken);

        try
        {
            if (await continuation.ApplyAsync(response, player, cancellationToken))
            {
                if (player.DailyChallenge.IsCompleted)
                {
                    player.ReplenishCrystals(2);
                }

                await players.ReplacePlayerAsync(player, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            ProgressTrackingFailed(exception, typeof(TCommand).Name, "apply");
        }

        return response;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Daily challenge progress tracking failed for {CommandType} during {Stage}")]
    private partial void ProgressTrackingFailed(Exception ex, string commandType, string stage);
}
