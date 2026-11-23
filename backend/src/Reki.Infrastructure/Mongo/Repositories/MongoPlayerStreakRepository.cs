using MongoDB.Driver;
using Reki.Application.Player.Streaks;
using Reki.Domain.Player;
using Reki.Domain.Player.Streaks;
using Reki.Infrastructure.Mongo.UnitOfWork;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPlayerStreakRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider) : IPlayerStreakRepository
{
    private readonly IMongoCollection<PlayerStreak> _streaks =
        database.GetCollection<PlayerStreak>(MongoCollections.PlayerStreaks);

    public async ValueTask<PlayerStreak?> GetLatestAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _streaks.Find(sessionProvider, item => item.PlayerId == playerId && item.IsActive)
            .SortByDescending(item => item.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask SaveAsync(PlayerStreak streak, CancellationToken cancellationToken) =>
        await _streaks.ReplaceOneAsync(
            sessionProvider,
            item => item.Id == streak.Id,
            streak,
            cancellationToken,
            new ReplaceOptions { IsUpsert = true });

    public async ValueTask DeleteForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _streaks.DeleteManyAsync(
            sessionProvider.Session,
            item => item.PlayerId == playerId,
            cancellationToken: cancellationToken);
}
