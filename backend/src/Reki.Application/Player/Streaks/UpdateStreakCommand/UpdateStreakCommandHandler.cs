using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Application.Player.Streaks.GetStreak;
using Reki.Domain.Player.Streaks;

namespace Reki.Application.Player.Streaks.UpdateStreakCommand;

internal sealed class UpdateStreakCommandHandler(
    ISender sender,
    IPlayerStreakRepository streaks,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    TimeProvider timeProvider) : ICommandHandler<UpdateStreak>
{
    public async ValueTask<Unit> Handle(UpdateStreak command, CancellationToken cancellationToken)
    {
        var streak = await sender.Send(new PlayerStreakQuery(command.PlayerId), cancellationToken);
        var now = timeProvider.GetUtcNow();

        var progress = streak.Progress(now);
        if (progress == PlayerStreakProgress.None)
        {
            return Unit.Value;
        }

        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await streaks.SaveAsync(streak, token);
                if (progress == PlayerStreakProgress.Stopped)
                {
                    await streaks.SaveAsync(PlayerStreak.New(command.PlayerId, now), token);
                }

                return Unit.Value;
            },
            _ => cache.Remove(CacheKeyPrefixes.PlayerStreak + command.PlayerId),
            cancellationToken);
        return Unit.Value;
    }
}
