using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class UpdateSettingsHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    PlayerReader playerReader)
    : IRequestHandler<UpdateSettingsCommand>
{
    public async ValueTask<Unit> Handle(
        UpdateSettingsCommand request,
        CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
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

        throw new RequestConflictException("The player changed while saving settings. Retry the request.");
    }
}