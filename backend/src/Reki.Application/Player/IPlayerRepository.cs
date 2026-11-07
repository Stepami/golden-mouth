using Reki.Domain.Player;

namespace Reki.Application.Player;

public interface IPlayerRepository
{
    ValueTask<PlayerProfile?> GetPlayerAsync(TelegramUserId telegramUserId, CancellationToken cancellationToken);

    ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask CreatePlayerAsync(
        PlayerProfile player,
        CancellationToken cancellationToken);

    ValueTask SavePlusNotificationAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken);

    ValueTask<bool> TryClaimPlusInterestAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken);

    ValueTask<bool> TryReplacePlayerAsync(
        PlayerProfile player,
        int expectedVersion,
        CancellationToken cancellationToken);

    ValueTask AddInviteBonusAsync(PlayerId playerId, int bonusAmount, CancellationToken cancellationToken);

    ValueTask DeletePlayerAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask<long> CountPlayersAsync(CancellationToken cancellationToken);

    ValueTask<long> ResetGameDayAsync(
        DateTimeOffset periodStart,
        int freeEnergy,
        int plusEnergy,
        CancellationToken cancellationToken);
}