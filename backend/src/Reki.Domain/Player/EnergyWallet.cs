namespace Reki.Domain.Player;

public static class EnergyWallet
{
    public const int Cap = 10;
    public const int RegenSeconds = 600;
    public const int InitialCrystals = 10;
    public const int ExchangeCost = 3;

    public static PlayerProfile Refresh(PlayerProfile player, DateTimeOffset now)
    {
        var anchor = player.EnergyUpdatedAtUtc ?? now;
        var energy = Math.Clamp(player.Energy, 0, Cap);
        if (energy < Cap && now > anchor)
        {
            var ticks = (long)((now - anchor).TotalSeconds / RegenSeconds);
            var gained = (int)Math.Min(Cap - energy, ticks);
            energy += gained;
            anchor = energy == Cap ? now : anchor.AddSeconds(gained * RegenSeconds);
        }

        if (player.Energy == energy && player.EnergyUpdatedAtUtc == anchor && player.Crystals is not null)
        {
            return player;
        }

        return player with
        {
            Energy = energy,
            EnergyUpdatedAtUtc = anchor,
            Crystals = player.Crystals ?? InitialCrystals,
            Version = player.Version + 1,
        };
    }

    public static PlayerProfile Exchange(PlayerProfile player, string requestId, DateTimeOffset now)
    {
        if (player.EnergyExchangeRequestIds.Contains(requestId))
        {
            return player;
        }

        if (player.Energy >= Cap || player.Crystals is null or < ExchangeCost)
        {
            throw new InvalidOperationException("Energy exchange is unavailable.");
        }

        return player with
        {
            Energy = Cap,
            EnergyUpdatedAtUtc = now,
            Crystals = player.Crystals.Value - ExchangeCost,
            EnergyExchangeRequestIds = [.. player.EnergyExchangeRequestIds, requestId],
            Version = player.Version + 1,
        };
    }
}
