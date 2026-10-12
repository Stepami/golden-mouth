using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Posts;
using Reki.Domain.Events;
using Reki.Domain.Posts;
using Reki.Domain.Social;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class InitialGameIndexes : RekiMigrationBase
{
    public override DatabaseVersion Version => new(1, 0, 0);

    public override async Task UpAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        // createIndexes adopts identical existing indexes and rejects conflicting definitions.
        var database = db;
        var players = database.GetCollection<BsonDocument>(MongoCollections.Players);
        var posts = database.GetCollection<GamePost>(MongoCollections.Posts);
        var publications = database.GetCollection<Publication>(MongoCollections.Publications);
        var memories = database.GetCollection<StarConversation>(MongoCollections.StarsMemory);
        var events = database.GetCollection<DailyGameEvent>(MongoCollections.Events);
        var challenges = database.GetCollection<GameChallenge>(MongoCollections.Challenges);
        var worlds = database.GetCollection<BsonDocument>(MongoCollections.Worlds);

        var telegramKey = Builders<BsonDocument>.IndexKeys
            .Ascending("TelegramUserId");
        var playerWorldKey = Builders<BsonDocument>.IndexKeys
            .Ascending("World");
        var postKeys = Builders<GamePost>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Descending(item => item.CreatedAtUtc);
        var memoryKeys = Builders<StarConversation>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Ascending(item => item.StarId);
        var eventKeys = Builders<DailyGameEvent>.IndexKeys
            .Ascending(item => item.PlayerId)
            .Descending(item => item.DayStartedAtUtc);
        var challengeKey = Builders<GameChallenge>.IndexKeys
            .Ascending(item => item.Participants);
        var worldKeys = Builders<BsonDocument>.IndexKeys
            .Ascending("PlayerId")
            .Ascending("WorldId");

        var telegramIndex = IndexFactory.Create(
            telegramKey,
            "ux_players_telegram_user",
            unique: true);
        var playerWorldIndex = IndexFactory.Create(
            playerWorldKey,
            "ix_players_world");
        var postIndex = IndexFactory.Create(postKeys, "ix_posts_player_created");
        var publicationExpiryIndex = new CreateIndexModel<Publication>(
            Builders<Publication>.IndexKeys.Ascending(item => item.ExpiresAtUtc),
            new CreateIndexOptions { Name = "ix_publications_expiry", ExpireAfter = TimeSpan.Zero });
        var memoryIndex = IndexFactory.Create(
            memoryKeys,
            "ux_stars_memory_player_star",
            unique: true);
        var eventIndex = IndexFactory.Create(eventKeys, "ix_events_player_day");
        var challengeOptions = new CreateIndexOptions<GameChallenge>
        {
            Name = "ux_challenges_active_participants",
            Unique = true,
            PartialFilterExpression = Builders<GameChallenge>.Filter
                .Eq(item => item.Status, ChallengeStatus.Active),
        };
        var challengeIndex = new CreateIndexModel<GameChallenge>(challengeKey, challengeOptions);
        var worldIndex = IndexFactory.Create(
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
            database,
            MongoCollections.Threads,
            "PlayerId",
            "ix_threads_player",
            cancellationToken);
        Task inviteTask = CreateDocumentIndexAsync(
            database,
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

    private static Task<string> CreateDocumentIndexAsync(
        IMongoDatabase database,
        string collectionName,
        string field,
        string indexName,
        CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<BsonDocument>(collectionName);
        var keys = Builders<BsonDocument>.IndexKeys.Ascending(field);
        var index = IndexFactory.Create(keys, indexName);
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