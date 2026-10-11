using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;

namespace Reki.Application.Player;

public sealed class UpdateSettingsHandler(IGameStore store, ISender sender) : IRequestHandler<UpdateSettingsCommand>
{
    public async ValueTask<Unit> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
            var updated = player with
            {
                Settings = player.Settings with
                {
                    Notifications = request.Notifications ?? player.Settings.Notifications,
                    Quiet = request.Quiet ?? player.Settings.Quiet,
                    Plus = request.Plus ?? player.Settings.Plus,
                },
                Address = request.Address ?? player.Address,
                Version = player.Version + 1,
            };
            if (await store.TryReplacePlayerAsync(updated, player.Version, cancellationToken))
            {
                return Unit.Value;
            }
        }

        throw new RequestConflictException("Settings changed concurrently.");
    }
}