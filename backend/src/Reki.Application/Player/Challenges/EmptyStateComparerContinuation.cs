using Reki.Domain.Player;

namespace Reki.Application.Player.Challenges;

internal sealed class EmptyStateComparerContinuation<TCommand, TResponse> :
    IStateComparer<TCommand, TResponse>.IContinuation
    where TCommand : ITrackDailyProgress
{
    public static readonly IStateComparer<TCommand, TResponse>.IContinuation Instance =
        new EmptyStateComparerContinuation<TCommand, TResponse>();

    public ValueTask<bool> ApplyAsync(
        TResponse response,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(false);
    }
}