using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.RekiContext;
using Reki.Domain.Player;

namespace Reki.Application.Player.UpdateSettingsCommand;

internal sealed class UpdateSettingsCommandHandler(
    IPlayerRepository playerRepository,
    IRekiContextAccessor rekiContextAccessor,
    IOptionsMonitor<GameBalance> balance)
    : ICommandHandler<UpdateSettings>
{
    public async ValueTask<Unit> Handle(UpdateSettings command, CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        var currentBalance = balance.CurrentValue;
        var updated = player with
        {
            Settings = new PlayerSettings(
                Notifications: command.Notifications ?? player.Settings.Notifications,
                Quiet: command.Quiet ?? player.Settings.Quiet,
                Plus: command.Plus ?? player.Settings.Plus),
            Address = command.Address ?? player.Address,
            Wallet = player.Wallet.Replenish(command.Plus is true && !player.Settings.Plus
                ? Math.Max(0, currentBalance.PlusPostsPerDay - currentBalance.FreePostsPerDay)
                : 0),
            Version = player.Version + 1,
        };
        await playerRepository.ReplacePlayerAsync(updated, cancellationToken);
        return Unit.Value;
    }
}
