namespace Reki.Application.Player.Responses;

/// <summary>Server-authoritative account energy and crystal balance.</summary>
public sealed record EnergyWalletResponse(
    int Amount,
    int Cap,
    DateTimeOffset? NextEnergyAtUtc,
    int RegenSeconds,
    int Crystals,
    EnergyExchangeQuoteResponse ExchangeQuote);
