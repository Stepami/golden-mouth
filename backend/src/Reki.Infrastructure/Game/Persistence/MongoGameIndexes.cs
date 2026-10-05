using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Game;

namespace Reki.Infrastructure.Game;

internal sealed class MongoGameIndexes(IMongoDatabase database) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<PlayerProfile> players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        IMongoCollection<GamePost> posts = database.GetCollection<GamePost>(MongoCollections.Posts);
        IMongoCollection<StarConversation> memories = database.GetCollection<StarConversation>(MongoCollections.StarsMemory);
        IMongoCollection<DailyGameEvent> events = database.GetCollection<DailyGameEvent>(MongoCollections.Events);
        IMongoCollection<GameChallenge> challenges = database.GetCollection<GameChallenge>(MongoCollections.Challenges);
        IMongoCollection<WorldRules> worlds = database.GetCollection<WorldRules>(MongoCollections.Worlds);
        IMongoCollection<AnalyticsBatch> analytics = database.GetCollection<AnalyticsBatch>(MongoCollections.Analytics);

        IndexKeysDefinition<PlayerProfile> telegramKey = Builders<PlayerProfile>.IndexKeys
            .Ascending(item => item.TelegramUserId);
        IndexKeysDefinition<GamePost> postKeys = Builders<GamePost>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Descending(item => item.CreatedAtUtc);
        IndexKeysDefinition<StarConversation> memoryKeys = Builders<StarConversation>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Ascending(item => item.StarId);
        IndexKeysDefinition<DailyGameEvent> eventKeys = Builders<DailyGameEvent>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Descending(item => item.DayStartedAtUtc);
        IndexKeysDefinition<GameChallenge> challengeKey = Builders<GameChallenge>.IndexKeys
            .Ascending(item => item.PlayerId);
        IndexKeysDefinition<WorldRules> worldKeys = Builders<WorldRules>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Ascending(item => item.WorldId);
        IndexKeysDefinition<AnalyticsBatch> analyticsKeys = Builders<AnalyticsBatch>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Descending(item => item.ReceivedAtUtc);

        CreateIndexModel<PlayerProfile> telegramIndex = IndexFactory.Create(
            telegramKey,
            "ux_players_telegram_user",
            unique: true);
        CreateIndexModel<GamePost> postIndex = IndexFactory.Create(postKeys, "ix_posts_player_created");
        CreateIndexModel<StarConversation> memoryIndex = IndexFactory.Create(
            memoryKeys,
            "ux_stars_memory_player_star",
            unique: true);
        CreateIndexModel<DailyGameEvent> eventIndex = IndexFactory.Create(eventKeys, "ix_events_player_day");
        CreateIndexModel<GameChallenge> challengeIndex = IndexFactory.Create(
            challengeKey,
            "ux_challenges_active_player",
            unique: true);
        CreateIndexModel<WorldRules> worldIndex = IndexFactory.Create(
            worldKeys,
            "ux_worlds_player_world",
            unique: true);
        CreateIndexModel<AnalyticsBatch> analyticsIndex = IndexFactory.Create(
            analyticsKeys,
            "ix_analytics_player_received");

        Task telegramTask = players.Indexes.CreateOneAsync(telegramIndex, cancellationToken: cancellationToken);
        Task postTask = posts.Indexes.CreateOneAsync(postIndex, cancellationToken: cancellationToken);
        Task memoryTask = memories.Indexes.CreateOneAsync(memoryIndex, cancellationToken: cancellationToken);
        Task eventTask = events.Indexes.CreateOneAsync(eventIndex, cancellationToken: cancellationToken);
        Task challengeTask = challenges.Indexes.CreateOneAsync(challengeIndex, cancellationToken: cancellationToken);
        Task worldTask = worlds.Indexes.CreateOneAsync(worldIndex, cancellationToken: cancellationToken);
        Task analyticsTask = analytics.Indexes.CreateOneAsync(analyticsIndex, cancellationToken: cancellationToken);
        Task threadTask = CreateDocumentIndexAsync(
            MongoCollections.Threads,
            "PlayerId",
            "ix_threads_player",
            cancellationToken);
        Task inviteTask = CreateDocumentIndexAsync(
            MongoCollections.Invites,
            "PlayerId",
            "ix_invites_player",
            cancellationToken);

        await Task.WhenAll(
            telegramTask,
            postTask,
            memoryTask,
            eventTask,
            challengeTask,
            worldTask,
            analyticsTask,
            threadTask,
            inviteTask);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task<string> CreateDocumentIndexAsync(
        string collectionName,
        string field,
        string indexName,
        CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(collectionName);
        IndexKeysDefinition<BsonDocument> keys = Builders<BsonDocument>.IndexKeys.Ascending(field);
        CreateIndexModel<BsonDocument> index = IndexFactory.Create(keys, indexName);
        return collection.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
    }

    private static class IndexFactory
    {
        public static CreateIndexModel<TDocument> Create<TDocument>(
            IndexKeysDefinition<TDocument> keys,
            string name,
            bool unique = false)
        {
            var options = new CreateIndexOptions { Name = name, Unique = unique };
            return new CreateIndexModel<TDocument>(keys, options);
        }
    }
}
