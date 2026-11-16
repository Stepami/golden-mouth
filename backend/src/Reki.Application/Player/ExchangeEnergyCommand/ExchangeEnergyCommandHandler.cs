using Mediator;
using Reki.Application.Player.Responses;
using Reki.Application.RekiContext;

namespace Reki.Application.Player.ExchangeEnergyCommand;

internal sealed class ExchangeEnergyCommandHandler(
    IUnitOfWork unitOfWork,
    IPlayerRepository players,
    IRekiContextAccessor contextAccessor,
    TimeProvider timeProvider) : ICommandHandler<ExchangeEnergy, EnergyExchangeResponse>
{
    public async ValueTask<EnergyExchangeResponse> Handle(ExchangeEnergy command, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                var player = await players.GetPlayerAsync(contextAccessor.Context.PlayerId, token);
                if (player is null || !player.Wallet.ExchangeAvailable)
                {
                    return new EnergyExchangeResponse(false, null);
                }

                player.ExchangeEnergy(timeProvider.GetUtcNow());
                await players.ReplacePlayerAsync(player, token);
                return new EnergyExchangeResponse(true, player.Wallet.ToResponse());
            },
            cancellationToken);
    }
}
