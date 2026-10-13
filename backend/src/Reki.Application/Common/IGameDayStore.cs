namespace Reki.Application.Common;

public interface IGameDayStore
{
    Task<GameDayResetResult> ResetAsync(DateTimeOffset periodStart, DateTimeOffset restedBefore, int restedPosts, CancellationToken cancellationToken);
}

public sealed record GameDayResetResult(long Players, long Worlds);
