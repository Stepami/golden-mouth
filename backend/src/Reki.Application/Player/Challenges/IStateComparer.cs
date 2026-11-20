using Reki.Application.RekiContext;
using Reki.Domain.Player.Challenges;

namespace Reki.Application.Player.Challenges;

internal interface IStateComparer<TCommand, TResponse>
{
    ValueTask<Func<TResponse, CancellationToken, ValueTask>?> CaptureAsync(
        TCommand command,
        IRekiContext context,
        PlayerChallenge challenge,
        CancellationToken cancellationToken);
}
