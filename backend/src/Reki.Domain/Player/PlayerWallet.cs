namespace Reki.Domain.Player;

public sealed class PlayerWallet(int energy, int crystals, DateTimeOffset energyUpdatedAtUtc)
{
    public const int Cap = 10;
    public const int RegenSeconds = 600;
    public const int InitialCrystals = 10;
    public const int ExchangeCost = 3;

    public int Energy { get; private set; } = energy;

    public int Crystals { get; private set; } = crystals;

    public DateTimeOffset EnergyUpdatedAtUtc { get; private set; } = energyUpdatedAtUtc;

    public DateTimeOffset? NextEnergyAtUtc => Energy < Cap ? EnergyUpdatedAtUtc.AddSeconds(RegenSeconds) : null;

    public string? ExchangeUnavailableReason => Energy >= Cap ? "energy_full" : InsufficientCrystalsReason;

    public bool ExchangeAvailable => ExchangeUnavailableReason is null;

    private string? InsufficientCrystalsReason => Crystals < ExchangeCost ? "insufficient_crystals" : null;

    public static PlayerWallet Create(DateTimeOffset now) => new(Cap, InitialCrystals, now.ToUniversalTime());

    internal void Spend(int cost, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cost);
        if (Energy < cost)
        {
            throw new InvalidOperationException("Insufficient energy.");
        }

        if (Energy == Cap)
        {
            EnergyUpdatedAtUtc = now.ToUniversalTime();
        }

        Energy -= cost;
    }

    internal void Replenish(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Crystals += amount;
    }

    internal bool TrySpendCrystals(int cost)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cost);
        if (Crystals < cost)
        {
            return false;
        }

        Crystals -= cost;
        return true;
    }

    internal bool Restore(DateTimeOffset now)
    {
        var ticks = (long)((now - EnergyUpdatedAtUtc).TotalSeconds / RegenSeconds);
        if (Energy >= Cap || ticks <= 0)
        {
            return false;
        }

        var gained = (int)Math.Min(Cap - Energy, ticks);
        Energy += gained;
        EnergyUpdatedAtUtc = Energy == Cap ? now.ToUniversalTime() : EnergyUpdatedAtUtc.AddSeconds(gained * RegenSeconds);
        return true;
    }

    internal void Exchange(DateTimeOffset now)
    {
        if (ExchangeUnavailableReason is not null)
        {
            throw new InvalidOperationException("Energy exchange is unavailable.");
        }

        Energy = Cap;
        Crystals -= ExchangeCost;
        EnergyUpdatedAtUtc = now.ToUniversalTime();
    }
}
