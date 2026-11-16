using Reki.Domain.Player;

namespace Reki.Application.Player;

public interface IPlayerRepository
{
    ValueTask<PlayerProfile?> GetPlayerAsync(ExternalUserId externalUserId, CancellationToken cancellationToken);

    ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask CreatePlayerAsync(
        PlayerProfile player,
        CancellationToken cancellationToken);

    ValueTask SavePlusNotificationAsync(
        PlayerProfile player,
        int previousVersion,
        CancellationToken cancellationToken);

    ValueTask<int> ClaimPlusInterestAsync(
        PlayerProfile player,
        int previousVersion,
        int previousCrystals,
        CancellationToken cancellationToken);

    ValueTask ReplacePlayerAsync(PlayerProfile player, CancellationToken cancellationToken);

    ValueTask DeletePlayerAsync(PlayerId playerId, CancellationToken cancellationToken);

    IAsyncEnumerable<PlayerProfile> GetPlayersForEnergyRecoveryAsync(int cap, DateTimeOffset dueBefore, CancellationToken cancellationToken);

    ValueTask<long> CountPlayersAsync(CancellationToken cancellationToken);
}
