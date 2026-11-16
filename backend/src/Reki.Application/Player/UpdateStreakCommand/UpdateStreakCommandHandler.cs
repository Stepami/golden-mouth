using Mediator;

namespace Reki.Application.Player.UpdateStreakCommand;

internal sealed class UpdateStreakCommandHandler : ICommandHandler<UpdateStreak>
{
    public ValueTask<Unit> Handle(UpdateStreak command, CancellationToken cancellationToken)
    {
        command.Player.UpdateStreak(command.Now);
        return ValueTask.FromResult(Unit.Value);
    }
}
