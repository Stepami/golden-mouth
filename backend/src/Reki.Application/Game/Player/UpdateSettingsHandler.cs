using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed class UpdateSettingsHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<UpdateSettingsCommand>
{
    public async ValueTask<Unit> Handle(
        UpdateSettingsCommand request,
        CancellationToken cancellationToken)
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
        await store.UpsertPlayerAsync(
            player with { Settings = settings, Version = player.Version + 1 },
            cancellationToken);
        return Unit.Value;
    }
}

