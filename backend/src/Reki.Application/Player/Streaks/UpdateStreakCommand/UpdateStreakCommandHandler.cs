using Mediator;
using Reki.Domain.Player.Streaks;

namespace Reki.Application.Player.Streaks.UpdateStreakCommand;

internal sealed class UpdateStreakCommandHandler(IPlayerStreakRepository streaks) : ICommandHandler<UpdateStreak, bool>
{
    public async ValueTask<bool> Handle(UpdateStreak command, CancellationToken cancellationToken)
    {
        var streak = await streaks.GetLatestAsync(command.PlayerId, cancellationToken);
        if (streak is { IsActive: true })
        {
            if (!streak.Progress(command.Now))
            {
                return false;
            }

            await streaks.SaveAsync(streak, cancellationToken);
            if (streak.IsActive)
            {
                return true;
            }
        }

        await streaks.SaveAsync(PlayerStreak.New(command.PlayerId, command.Now), cancellationToken);
        return true;
    }
}
