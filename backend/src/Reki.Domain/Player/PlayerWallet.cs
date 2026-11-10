namespace Reki.Domain.Player;

public sealed record PlayerWallet
{
    public const int Cap = 10;
    public const int RegenSeconds = 600;
    public const int InitialCrystals = 10;
    public const int ExchangeCost = 3;

    public int Energy { get; init; }

    public int EnergyUsed { get; init; }

    public int Crystals { get; init; }

    public DateTimeOffset EnergyUpdatedAtUtc { get; init; }

    public DateTimeOffset? NextEnergyAtUtc => Energy < Cap ? EnergyUpdatedAtUtc.AddSeconds(RegenSeconds) : null;

    public string? ExchangeUnavailableReason => Energy >= Cap ? "energy_full" : InsufficientCrystalsReason;

    public bool ExchangeAvailable => ExchangeUnavailableReason is null;

    private string? InsufficientCrystalsReason => Crystals < ExchangeCost ? "insufficient_crystals" : null;

    public static PlayerWallet Create(DateTimeOffset now) => new()
    {
        Energy = Cap,
        Crystals = InitialCrystals,
        EnergyUpdatedAtUtc = now.ToUniversalTime(),
    };

    public PlayerWallet Spend(int cost, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cost);
        if (Energy < cost)
        {
            throw new InvalidOperationException("Insufficient energy.");
        }

        return this with
        {
            Energy = Energy - cost,
            EnergyUsed = EnergyUsed + cost,
            EnergyUpdatedAtUtc = Energy == Cap ? now.ToUniversalTime() : EnergyUpdatedAtUtc,
        };
    }

    public PlayerWallet Replenish(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return this with { Energy = Math.Min(Cap, Energy + amount) };
    }

    public PlayerWallet Restore(DateTimeOffset now)
    {
        var ticks = (long)((now - EnergyUpdatedAtUtc).TotalSeconds / RegenSeconds);
        if (Energy >= Cap || ticks <= 0)
        {
            return this;
        }

        var gained = (int)Math.Min(Cap - Energy, ticks);
        var energy = Energy + gained;
        return this with
        {
            Energy = energy,
            EnergyUpdatedAtUtc = energy == Cap ? now.ToUniversalTime() : EnergyUpdatedAtUtc.AddSeconds(gained * RegenSeconds),
        };
    }

    public PlayerWallet Exchange(DateTimeOffset now)
    {
        if (ExchangeUnavailableReason is not null)
        {
            throw new InvalidOperationException("Energy exchange is unavailable.");
        }

        return this with { Energy = Cap, Crystals = Crystals - ExchangeCost, EnergyUpdatedAtUtc = now.ToUniversalTime() };
    }
}
