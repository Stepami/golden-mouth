using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Posts;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Persistence;

internal sealed class MongoGameIndexes(IMongoDatabase database) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<PlayerProfile> players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        IMongoCollection<GamePost> posts = database.GetCollection<GamePost>(MongoCollections.Posts);
        IMongoCollection<Publication> publications = database.GetCollection<Publication>(MongoCollections.Publications);
        IMongoCollection<StarConversation> memories = database.GetCollection<StarConversation>(MongoCollections.StarsMemory);
        IMongoCollection<DailyGameEvent> events = database.GetCollection<DailyGameEvent>(MongoCollections.Events);
        IMongoCollection<GameChallenge> challenges = database.GetCollection<GameChallenge>(MongoCollections.Challenges);
        IMongoCollection<WorldRules> worlds = database.GetCollection<WorldRules>(MongoCollections.Worlds);

        IndexKeysDefinition<PlayerProfile> telegramKey = Builders<PlayerProfile>.IndexKeys
            .Ascending(item => item.TelegramUserId);
        IndexKeysDefinition<PlayerProfile> playerWorldKey = Builders<PlayerProfile>.IndexKeys
            .Ascending(item => item.World);
        IndexKeysDefinition<GamePost> postKeys = Builders<GamePost>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Descending(item => item.CreatedAtUtc);
        IndexKeysDefinition<StarConversation> memoryKeys = Builders<StarConversation>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Ascending(item => item.StarId);
        IndexKeysDefinition<DailyGameEvent> eventKeys = Builders<DailyGameEvent>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Descending(item => item.DayStartedAtUtc);
        await DropLegacyChallengeIndexAsync(challenges, cancellationToken);
        IndexKeysDefinition<GameChallenge> challengeKey = Builders<GameChallenge>.IndexKeys
            .Ascending(item => item.Participants);
        IndexKeysDefinition<WorldRules> worldKeys = Builders<WorldRules>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Ascending(item => item.WorldId);

        CreateIndexModel<PlayerProfile> telegramIndex = IndexFactory.Create(
            telegramKey,
            "ux_players_telegram_user",
            unique: true);
        CreateIndexModel<PlayerProfile> playerWorldIndex = IndexFactory.Create(
            playerWorldKey,
            "ix_players_world");
        CreateIndexModel<GamePost> postIndex = IndexFactory.Create(postKeys, "ix_posts_player_created");
        var publicationExpiryIndex = new CreateIndexModel<Publication>(
            Builders<Publication>.IndexKeys.Ascending(item => item.ExpiresAtUtc),
            new CreateIndexOptions { Name = "ix_publications_expiry", ExpireAfter = TimeSpan.Zero });
        CreateIndexModel<StarConversation> memoryIndex = IndexFactory.Create(
            memoryKeys,
            "ux_stars_memory_player_star",
            unique: true);
        CreateIndexModel<DailyGameEvent> eventIndex = IndexFactory.Create(eventKeys, "ix_events_player_day");
        var challengeOptions = new CreateIndexOptions<GameChallenge>
        {
            Name = "ux_challenges_active_participants",
            Unique = true,
            PartialFilterExpression = Builders<GameChallenge>.Filter
                .Eq(item => item.Status, ChallengeStatus.Active),
        };
        var challengeIndex = new CreateIndexModel<GameChallenge>(challengeKey, challengeOptions);
        CreateIndexModel<WorldRules> worldIndex = IndexFactory.Create(
            worldKeys,
            "ux_worlds_player_world",
            unique: true);

        Task telegramTask = players.Indexes.CreateOneAsync(telegramIndex, cancellationToken: cancellationToken);
        Task playerWorldTask = players.Indexes.CreateOneAsync(
            playerWorldIndex,
            cancellationToken: cancellationToken);
        Task postTask = posts.Indexes.CreateOneAsync(postIndex, cancellationToken: cancellationToken);
        Task publicationTask = publications.Indexes.CreateOneAsync(
            publicationExpiryIndex,
            cancellationToken: cancellationToken);
        Task memoryTask = memories.Indexes.CreateOneAsync(memoryIndex, cancellationToken: cancellationToken);
        Task eventTask = events.Indexes.CreateOneAsync(eventIndex, cancellationToken: cancellationToken);
        Task challengeTask = challenges.Indexes.CreateOneAsync(challengeIndex, cancellationToken: cancellationToken);
        Task worldTask = worlds.Indexes.CreateOneAsync(worldIndex, cancellationToken: cancellationToken);
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
            playerWorldTask,
            postTask,
            publicationTask,
            memoryTask,
            eventTask,
            challengeTask,
            worldTask,
            threadTask,
            inviteTask);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task DropLegacyChallengeIndexAsync(
        IMongoCollection<GameChallenge> challenges,
        CancellationToken cancellationToken)
    {
        using IAsyncCursor<BsonDocument> cursor = await challenges.Indexes.ListAsync(cancellationToken);
        IReadOnlyList<BsonDocument> indexes = await cursor.ToListAsync(cancellationToken);
        if (indexes.Any(index =>
            index.TryGetValue("name", out BsonValue? name) &&
            name.AsString == "ux_challenges_active_player"))
        {
            await challenges.Indexes.DropOneAsync(
                "ux_challenges_active_player",
                cancellationToken);
        }
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
