using Mediator;
using Reki.Application.Player.Responses;

namespace Reki.Application.Player.ExchangeEnergyCommand;

public sealed record ExchangeEnergy(string RequestId, int StateVersion) : ICommand<EnergyWalletResponse>;
