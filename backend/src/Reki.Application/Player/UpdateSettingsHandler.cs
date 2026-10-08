using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed class UpdateSettingsHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<UpdateSettingsCommand>
{
    public async ValueTask<Unit> Handle(
        UpdateSettingsCommand request,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
            PlayerSettings settings = player.Settings with
            {
                Drama = request.Drama ?? player.Settings.Drama,
                Notifications = request.Notifications ?? player.Settings.Notifications,
                Quiet = request.Quiet ?? player.Settings.Quiet,
                Voice = request.Voice ?? player.Settings.Voice,
                Plus = request.Plus ?? player.Settings.Plus,
            };
            PlayerProfile updated = player with
            {
                Settings = settings,
                Address = request.Address ?? player.Address,
                Version = player.Version + 1,
            };
            if (await store.TryReplacePlayerAsync(updated, player.Version, cancellationToken))
            {
                return Unit.Value;
            }
        }

        throw new RequestConflictException("The player changed while saving settings. Retry the request.");
    }
}

