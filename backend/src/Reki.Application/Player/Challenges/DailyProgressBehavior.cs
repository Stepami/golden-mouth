using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.RekiContext;

namespace Reki.Application.Player.Challenges;

internal sealed partial class DailyProgressBehavior<TCommand, TResponse>(
    IStateComparer<TCommand, TResponse> comparer,
    IRekiContextAccessor contextAccessor,
    IPlayerRepository players,
    IUnitOfWork unitOfWork,
    IOptionsMonitor<GameBalance> balance,
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
        if (continuation == EmptyStateComparerContinuation<TCommand, TResponse>.Instance)
        {
            return response;
        }

        try
        {
            await unitOfWork.ExecuteAsync(
                async token =>
                {
                    var current = await players.GetPlayerAsync(player.Id, token);
                    if (current is null || current.DailyChallenge.Day != day || current.DailyChallenge.IsCompleted ||
                        !await continuation.ApplyAsync(response, current, token))
                    {
                        return Unit.Value;
                    }

                    if (current.DailyChallenge.IsCompleted)
                    {
                        current.ReplenishCrystals(balance.CurrentValue.Bonuses.ChallengeCrystals);
                    }

                    await players.ReplacePlayerAsync(current, token);
                    return Unit.Value;
                },
                cancellationToken);
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
