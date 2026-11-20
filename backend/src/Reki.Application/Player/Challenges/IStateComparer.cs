using Reki.Domain.Player;

namespace Reki.Application.Player.Challenges;

internal interface IStateComparer<TCommand, TResponse>
    where TCommand : ITrackDailyProgress
{
    internal interface IContinuation
    {
        ValueTask<bool> ApplyAsync(
            TResponse response,
            PlayerProfile player,
            CancellationToken cancellationToken);
    }

    ValueTask<IContinuation> CaptureAsync(
        TCommand command,
        PlayerProfile player,
        CancellationToken cancellationToken);
}