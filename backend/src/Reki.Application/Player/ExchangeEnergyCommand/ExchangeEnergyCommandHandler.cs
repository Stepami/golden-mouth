using Mediator;
using Reki.Application.Player.Responses;
using Reki.Application.RekiContext;

namespace Reki.Application.Player.ExchangeEnergyCommand;

internal sealed class ExchangeEnergyCommandHandler(
    IPlayerRepository players,
    IRekiContextAccessor contextAccessor,
    TimeProvider timeProvider) : ICommandHandler<ExchangeEnergy, EnergyExchangeResponse>
{
    public async ValueTask<EnergyExchangeResponse> Handle(ExchangeEnergy request, CancellationToken cancellationToken)
    {
        var player = await contextAccessor.Context.Player;
        var now = timeProvider.GetUtcNow();
        var previous = await players.TryExchangeEnergyAsync(player.Id, now, cancellationToken);
        return previous is null
            ? new EnergyExchangeResponse(false, null)
            : new EnergyExchangeResponse(true, previous.Exchange(now).ToResponse());
    }
}
