using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Persistence;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

public sealed partial class ResetGameDayJob(
    IMongoDatabase database,
    GameDayPeriod gameDayPeriod,
    IOptions<GameBalance> balance,
    ILogger<ResetGameDayJob> logger) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var periodStart = gameDayPeriod.PeriodStart;
        var growth = balance.Value.Growth;
        var restedBefore = periodStart.AddDays(-growth.RestedAfterSkippedDays);
        var restedPosts = growth.RestedFirstPosts;
        var players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        var worlds = database.GetCollection<World>(MongoCollections.Worlds);
        var playerResult = await players.UpdateManyAsync(
            Builders<PlayerProfile>.Filter.Lt(player => player.PostsPeriodStartedAtUtc, periodStart),
            Builders<PlayerProfile>.Update
                .Set(player => player.EnergyUsed, 0)
                .Set(player => player.DailyBonus, 0)
                .Set(player => player.PostsPeriodStartedAtUtc, periodStart)
                .Inc(player => player.Version, 1),
            cancellationToken: cancellationToken);
        var period = SerializeDate(periodStart);
        var cutoff = SerializeDate(restedBefore);
        var rested = new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$and", new BsonArray
            {
                new BsonDocument("$ne", new BsonArray { "$Player.LastPostAtUtc", BsonNull.Value }),
                new BsonDocument("$lt", new BsonArray { "$Player.LastPostAtUtc", new BsonDocument("$literal", cutoff) }),
            }),
            restedPosts,
            0,
        });
        var starChanges = new BsonDocument
        {
            { "RelationGainedInPeriod", 0 },
            { "RelationPeriodStartedAtUtc", new BsonDocument("$literal", period) },
        };
        var update = new BsonDocument("$set", new BsonDocument
        {
            { "Player.PeriodStartedAtUtc", new BsonDocument("$literal", period) },
            { "Player.RestedPostsRemaining", rested },
            { "Player.Wants", MapArray("$Player.Wants", new BsonDocument("Done", false)) },
            { "Stars", MapArray("$Stars", starChanges) },
            { "Version", new BsonDocument("$add", new BsonArray { "$Version", 1 }) },
        });
        var worldResult = await worlds.UpdateManyAsync(
            Builders<World>.Filter.Lt(world => world.Player.PeriodStartedAtUtc, periodStart),
            Builders<World>.Update.Pipeline(new[] { update }),
            cancellationToken: cancellationToken);

        LogResetCompleted(playerResult.ModifiedCount, worldResult.ModifiedCount, periodStart);
    }

    private static BsonDocument MapArray(string field, BsonDocument changes) => new("$map", new BsonDocument
    {
        { "input", field },
        { "as", "item" },
        { "in", new BsonDocument("$mergeObjects", new BsonArray { "$$item", changes }) },
    });

    private static BsonValue SerializeDate(DateTimeOffset value)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        writer.WriteStartDocument();
        writer.WriteName("value");
        BsonSerializer.LookupSerializer<DateTimeOffset>().Serialize(BsonSerializationContext.CreateRoot(writer), value);
        writer.WriteEndDocument();
        return document["value"];
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Game day reset completed: {Players} players, {Worlds} worlds, period {PeriodStart}")]
    private partial void LogResetCompleted(long players, long worlds, DateTimeOffset periodStart);
}
