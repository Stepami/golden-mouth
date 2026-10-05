using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Common;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Persistence;

internal sealed class MongoGameStore : IGameStore
{
    private readonly IMongoCollection<PlayerProfile> players;
    private readonly IMongoCollection<GamePost> posts;
    private readonly IMongoCollection<StarConversation> starMemories;
    private readonly IMongoCollection<DailyGameEvent> events;
    private readonly IMongoCollection<GameChallenge> challenges;
    private readonly IMongoCollection<WorldRules> worlds;
    private readonly IMongoCollection<AnalyticsBatch> analytics;
    private readonly IMongoCollection<BsonDocument> threads;
    private readonly IMongoCollection<BsonDocument> invites;
    private readonly IMongoCollection<BsonDocument> logs;

    public MongoGameStore(IMongoDatabase database, IOptions<MongoOptions> options)
    {
        players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        posts = database.GetCollection<GamePost>(MongoCollections.Posts);
        starMemories = database.GetCollection<StarConversation>(MongoCollections.StarsMemory);
        events = database.GetCollection<DailyGameEvent>(MongoCollections.Events);
        challenges = database.GetCollection<GameChallenge>(MongoCollections.Challenges);
        worlds = database.GetCollection<WorldRules>(MongoCollections.Worlds);
        analytics = database.GetCollection<AnalyticsBatch>(MongoCollections.Analytics);
        threads = database.GetCollection<BsonDocument>(MongoCollections.Threads);
        invites = database.GetCollection<BsonDocument>(MongoCollections.Invites);
        logs = database.GetCollection<BsonDocument>(options.Value.LogCollectionName);
    }

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        return await players
            .Find(player => player.TelegramUserId == telegramUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask UpsertPlayerAsync(
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await players.ReplaceOneAsync(
            item => item.TelegramUserId == player.TelegramUserId,
            player,
            options,
            cancellationToken);
    }

    public async ValueTask<bool> TryReplacePlayerAsync(
        PlayerProfile player,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        FilterDefinition<PlayerProfile> telegramFilter = Builders<PlayerProfile>.Filter
            .Eq(item => item.TelegramUserId, player.TelegramUserId);
        FilterDefinition<PlayerProfile> versionFilter = Builders<PlayerProfile>.Filter
            .Eq(item => item.Version, expectedVersion);
        FilterDefinition<PlayerProfile> filter = Builders<PlayerProfile>.Filter
            .And(telegramFilter, versionFilter);
        ReplaceOneResult result = await players.ReplaceOneAsync(
            filter,
            player,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async ValueTask DeletePlayerAsync(
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        FilterDefinition<BsonDocument> threadFilter = Builders<BsonDocument>.Filter
            .Eq("PlayerId", player.Id);
        FilterDefinition<BsonDocument> logFilter = Builders<BsonDocument>.Filter
            .Eq("Properties.Player", player.Id);
        Task deletePlayer = players.DeleteOneAsync(
            item => item.TelegramUserId == player.TelegramUserId,
            cancellationToken);
        Task deletePosts = posts.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deleteMemories = starMemories.DeleteManyAsync(
            item => item.PlayerId == player.Id,
            cancellationToken);
        Task deleteEvents = events.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deleteChallenges = challenges.DeleteManyAsync(
            item => item.PlayerId == player.Id,
            cancellationToken);
        Task deleteWorlds = worlds.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deleteAnalytics = analytics.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deleteThreads = threads.DeleteManyAsync(threadFilter, cancellationToken);
        Task deleteInvites = invites.DeleteManyAsync(threadFilter, cancellationToken);
        Task deleteLogs = logs.DeleteManyAsync(logFilter, cancellationToken);

        await Task.WhenAll(
            deletePlayer,
            deletePosts,
            deleteMemories,
            deleteEvents,
            deleteChallenges,
            deleteWorlds,
            deleteAnalytics,
            deleteThreads,
            deleteInvites,
            deleteLogs);
    }

    public async ValueTask AddPostAsync(GamePost post, CancellationToken cancellationToken)
    {
        await posts.InsertOneAsync(post, cancellationToken: cancellationToken);
    }

    public async ValueTask<IReadOnlyList<GamePost>> GetRecentPostsAsync(
        string playerId,
        int count,
        CancellationToken cancellationToken)
    {
        return await posts
            .Find(item => item.PlayerId == playerId)
            .SortByDescending(item => item.CreatedAtUtc)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<GamePost?> GetPostAsync(
        string playerId,
        string postId,
        CancellationToken cancellationToken)
    {
        return await posts
            .Find(item => item.PlayerId == playerId && item.Id == postId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<bool> AddReplyAsync(
        string playerId,
        string postId,
        GameComment reply,
        CancellationToken cancellationToken)
    {
        FilterDefinition<GamePost> playerFilter = Builders<GamePost>.Filter
            .Eq(item => item.PlayerId, playerId);
        FilterDefinition<GamePost> postFilter = Builders<GamePost>.Filter.Eq(item => item.Id, postId);
        FilterDefinition<GamePost> filter = Builders<GamePost>.Filter.And(playerFilter, postFilter);
        UpdateDefinition<GamePost> update = Builders<GamePost>.Update.Push(item => item.Comments, reply);
        UpdateResult result = await posts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async ValueTask<StarConversation?> GetStarConversationAsync(
        string playerId,
        string starId,
        CancellationToken cancellationToken)
    {
        return await starMemories
            .Find(item => item.PlayerId == playerId && item.StarId == starId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveStarConversationAsync(
        StarConversation conversation,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await starMemories.ReplaceOneAsync(
            item => item.PlayerId == conversation.PlayerId && item.StarId == conversation.StarId,
            conversation,
            options,
            cancellationToken);
    }

    public async ValueTask<DailyGameEvent?> GetTodayEventAsync(
        string playerId,
        DateTimeOffset dayStartedAtUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinition<DailyGameEvent> filter = Builders<DailyGameEvent>.Filter
            .Eq(item => item.PlayerId, playerId);
        if (dayStartedAtUtc != DateTimeOffset.MinValue)
        {
            FilterDefinition<DailyGameEvent> dayFilter = Builders<DailyGameEvent>.Filter
                .Eq(item => item.DayStartedAtUtc, dayStartedAtUtc);
            filter = Builders<DailyGameEvent>.Filter.And(filter, dayFilter);
        }

        return await events
            .Find(filter)
            .SortByDescending(item => item.DayStartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveEventAsync(
        DailyGameEvent gameEvent,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await events.ReplaceOneAsync(
            item => item.Id == gameEvent.Id,
            gameEvent,
            options,
            cancellationToken);
    }

    public async ValueTask<GameChallenge?> GetActiveChallengeAsync(
        string playerId,
        CancellationToken cancellationToken)
    {
        return await challenges.Find(item => item.PlayerId == playerId).FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveChallengeAsync(
        GameChallenge challenge,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await challenges.ReplaceOneAsync(
            item => item.Id == challenge.Id,
            challenge,
            options,
            cancellationToken);
    }

    public async ValueTask<WorldRules?> GetWorldRulesAsync(
        string playerId,
        string worldId,
        CancellationToken cancellationToken)
    {
        return await worlds
            .Find(item => item.PlayerId == playerId && item.WorldId == worldId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveWorldRulesAsync(
        WorldRules rules,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await worlds.ReplaceOneAsync(
            item => item.PlayerId == rules.PlayerId && item.WorldId == rules.WorldId,
            rules,
            options,
            cancellationToken);
    }

    public async ValueTask AppendAnalyticsAsync(
        AnalyticsBatch batch,
        CancellationToken cancellationToken)
    {
        await analytics.InsertOneAsync(batch, cancellationToken: cancellationToken);
    }

    public async ValueTask AppendThreadAsync(
        string playerId,
        string postId,
        string hook,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        var document = new BsonDocument
        {
            ["PlayerId"] = playerId,
            ["PostId"] = postId,
            ["Hook"] = hook,
            ["CreatedAtUtc"] = createdAtUtc.UtcDateTime,
            ["Open"] = true,
        };
        await threads.InsertOneAsync(document, cancellationToken: cancellationToken);
    }
}
