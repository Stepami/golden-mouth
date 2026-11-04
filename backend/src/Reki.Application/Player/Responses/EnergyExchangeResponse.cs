namespace Reki.Application.Player.Responses;

/// <summary>Expected exchange result. Refresh the wallet after an unavailable operation or a lost response.</summary>
public sealed record EnergyExchangeResponse(bool Exchanged, EnergyWalletResponse? Wallet);
