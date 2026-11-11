using Mediator;

namespace Reki.Application.Player.UpdateLastSeenAtCommand;

internal sealed class UpdateLastSeenAtCommandHandler(IPlayerRepository players, TimeProvider timeProvider)
    : ICommandHandler<UpdateLastSeenAt>
{
    public async ValueTask<Unit> Handle(UpdateLastSeenAt command, CancellationToken cancellationToken)
    {
        var player = command.Player with { LastSeenAt = timeProvider.GetUtcNow() };
        await players.ReplacePlayerAsync(player, cancellationToken);
        return Unit.Value;
    }
}
