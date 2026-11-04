namespace Reki.Application.Player.Responses;

/// <summary>Fixed price to refill energy to the cap, not add ten to the balance.</summary>
public sealed record EnergyExchangeQuoteResponse(int CrystalCost, int TargetEnergy, bool Available, string? UnavailableReason);
