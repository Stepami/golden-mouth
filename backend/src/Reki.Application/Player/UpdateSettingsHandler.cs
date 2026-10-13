using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed class UpdateSettingsHandler(
    IGameStore store,
    IRekiContextAccessor rekiContextAccessor,
    IOptions<GameBalance> balance)
    : IRequestHandler<UpdateSettingsCommand>
{
    public async ValueTask<Unit> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        var updated = player with
        {
            Settings = new PlayerSettings(
                Notifications: request.Notifications ?? player.Settings.Notifications,
                Quiet: request.Quiet ?? player.Settings.Quiet,
                Plus: request.Plus ?? player.Settings.Plus),
            Address = request.Address ?? player.Address,
            Energy = player.Energy + (request.Plus is true && !player.Settings.Plus
                ? Math.Max(0, balance.Value.PlusPostsPerDay - balance.Value.FreePostsPerDay)
                : 0),
            Version = player.Version + 1,
        };
        await store.TryReplacePlayerAsync(updated, player.Version, cancellationToken);
        return Unit.Value;
    }
}