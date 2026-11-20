using Reki.Domain.Player;

namespace Reki.Application.Player.Challenges;

internal interface IStateComparer<TCommand, TResponse>
{
    ValueTask<Func<TResponse, CancellationToken, ValueTask>> CaptureAsync(
        TCommand command,
        PlayerProfile player,
        CancellationToken cancellationToken);
}
