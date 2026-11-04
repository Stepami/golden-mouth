using Mediator;
using Reki.Application.Player.Responses;
using Reki.Application.RekiContext;

namespace Reki.Application.Player.GetEnergyWallet;

internal sealed class EnergyWalletQueryHandler(IRekiContextAccessor contextAccessor)
    : IQueryHandler<EnergyWalletQuery, EnergyWalletResponse>
{
    public async ValueTask<EnergyWalletResponse> Handle(EnergyWalletQuery request, CancellationToken cancellationToken)
    {
        var player = await contextAccessor.Context.Player;
        return player.ToEnergyWalletResponse();
    }
}
