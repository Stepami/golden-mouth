using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Persistence;

internal sealed class GameDayIndexes(MongoCompositionRoot compositionRoot) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var database = compositionRoot.MongoDatabase;
        await database.GetCollection<PlayerProfile>(MongoCollections.Players).Indexes.CreateOneAsync(
            new CreateIndexModel<PlayerProfile>(
                Builders<PlayerProfile>.IndexKeys.Ascending(player => player.PostsPeriodStartedAtUtc),
                new CreateIndexOptions { Name = "ix_players_day_period" }),
            cancellationToken: cancellationToken);
        await database.GetCollection<World>(MongoCollections.Worlds).Indexes.CreateOneAsync(
            new CreateIndexModel<World>(
                Builders<World>.IndexKeys.Ascending(world => world.Player.PeriodStartedAtUtc),
                new CreateIndexOptions { Name = "ix_worlds_day_period" }),
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
