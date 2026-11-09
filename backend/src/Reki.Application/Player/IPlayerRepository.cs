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
        PlayerProfile updated,
        CancellationToken cancellationToken);

    ValueTask ClaimPlusInterestAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken);

    ValueTask ReplacePlayerAsync(PlayerProfile player, CancellationToken cancellationToken);

    ValueTask AddInviteBonusAsync(PlayerId playerId, int bonusAmount, CancellationToken cancellationToken);

    ValueTask DeletePlayerAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask<long> CountPlayersAsync(CancellationToken cancellationToken);

    ValueTask<long> ResetGameDayAsync(
        DateTimeOffset periodStart,
        int freeEnergy,
        int plusEnergy,
        CancellationToken cancellationToken);
}
