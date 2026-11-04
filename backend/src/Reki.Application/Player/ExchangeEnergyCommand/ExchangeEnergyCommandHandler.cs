using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Player.Responses;
using Reki.Application.RekiContext;
using Reki.Domain.Player;

namespace Reki.Application.Player.ExchangeEnergyCommand;

internal sealed class ExchangeEnergyCommandHandler(
    IPlayerRepository players,
    IRekiContextAccessor contextAccessor,
    TimeProvider timeProvider) : ICommandHandler<ExchangeEnergy, EnergyWalletResponse>
{
    public async ValueTask<EnergyWalletResponse> Handle(ExchangeEnergy request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 64)
        {
            throw new RequestConflictException("A request ID of at most 64 characters is required.");
        }

        var player = await players.GetPlayerAsync(contextAccessor.Context.TelegramUserId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
        if (player.EnergyExchangeRequestIds.Contains(request.RequestId))
        {
            return player.ToEnergyWalletResponse();
        }

        var snapshot = player.ToEnergyWalletResponse();
        if (player.Version != request.StateVersion || !snapshot.ExchangeQuote.Available)
        {
            throw new RequestConflictException("Refresh the wallet before exchanging energy.");
        }

        var updated = EnergyWallet.Exchange(player, request.RequestId, timeProvider.GetUtcNow());
        if (await players.TrySaveWalletAsync(player, updated, cancellationToken))
        {
            return updated.ToEnergyWalletResponse();
        }

        // A concurrent retry of this same operation may have committed successfully.
        var current = await players.GetPlayerAsync(player.Id, cancellationToken);
        if (current is not null && current.EnergyExchangeRequestIds.Contains(request.RequestId))
        {
            return current.ToEnergyWalletResponse();
        }

        throw new RequestConflictException("The wallet changed. Refresh before retrying.");
    }
}
