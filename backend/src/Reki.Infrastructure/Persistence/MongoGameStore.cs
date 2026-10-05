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
    private readonly IMongoCollection<PlayerProfile> _players;
    private readonly IMongoCollection<GamePost> _posts;
    private readonly IMongoCollection<StarConversation> _starMemories;
    private readonly IMongoCollection<DailyGameEvent> _events;
    private readonly IMongoCollection<GameChallenge> _challenges;
    private readonly IMongoCollection<WorldRules> _worlds;
    private readonly IMongoCollection<AnalyticsBatch> _analytics;
    private readonly IMongoCollection<BsonDocument> _threads;
    private readonly IMongoCollection<BsonDocument> _invites;
    private readonly IMongoCollection<BsonDocument> _logs;

    public MongoGameStore(IMongoDatabase database, IOptions<MongoOptions> options)
    {
        _players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        _posts = database.GetCollection<GamePost>(MongoCollections.Posts);
        _starMemories = database.GetCollection<StarConversation>(MongoCollections.StarsMemory);
        _events = database.GetCollection<DailyGameEvent>(MongoCollections.Events);
        _challenges = database.GetCollection<GameChallenge>(MongoCollections.Challenges);
        _worlds = database.GetCollection<WorldRules>(MongoCollections.Worlds);
        _analytics = database.GetCollection<AnalyticsBatch>(MongoCollections.Analytics);
        _threads = database.GetCollection<BsonDocument>(MongoCollections.Threads);
        _invites = database.GetCollection<BsonDocument>(MongoCollections.Invites);
        _logs = database.GetCollection<BsonDocument>(options.Value.LogCollectionName);
    }

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        TelegramUserId telegramUserId,
        CancellationToken cancellationToken)
    {
        return await _players
            .Find(player => player.TelegramUserId == telegramUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask UpsertPlayerAsync(
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await _players.ReplaceOneAsync(
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
        ReplaceOneResult result = await _players.ReplaceOneAsync(
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
            .Eq("PlayerId", player.Id.Value);
        FilterDefinition<BsonDocument> logFilter = Builders<BsonDocument>.Filter
            .Eq("Properties.Player", player.Id.Value);
        Task deletePlayer = _players.DeleteOneAsync(
            item => item.TelegramUserId == player.TelegramUserId,
            cancellationToken);
        Task deletePosts = _posts.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deleteMemories = _starMemories.DeleteManyAsync(
            item => item.PlayerId == player.Id,
            cancellationToken);
        Task deleteEvents = _events.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deleteChallenges = _challenges.DeleteManyAsync(
            item => item.PlayerId == player.Id,
            cancellationToken);
        Task deleteWorlds = _worlds.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deleteAnalytics = _analytics.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deleteThreads = _threads.DeleteManyAsync(threadFilter, cancellationToken);
        Task deleteInvites = _invites.DeleteManyAsync(threadFilter, cancellationToken);
        Task deleteLogs = _logs.DeleteManyAsync(logFilter, cancellationToken);

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
        await _posts.InsertOneAsync(post, cancellationToken: cancellationToken);
    }

    public async ValueTask<IReadOnlyList<GamePost>> GetRecentPostsAsync(
        PlayerId playerId,
        int count,
        CancellationToken cancellationToken)
    {
        return await _posts
            .Find(item => item.PlayerId == playerId)
            .SortByDescending(item => item.CreatedAtUtc)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<GamePost?> GetPostAsync(
        PlayerId playerId,
        PostId postId,
        CancellationToken cancellationToken)
    {
        return await _posts
            .Find(item => item.PlayerId == playerId && item.Id == postId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<bool> AddReplyAsync(
        PlayerId playerId,
        PostId postId,
        GameComment reply,
        CancellationToken cancellationToken)
    {
        FilterDefinition<GamePost> playerFilter = Builders<GamePost>.Filter
            .Eq(item => item.PlayerId, playerId);
        FilterDefinition<GamePost> postFilter = Builders<GamePost>.Filter.Eq(item => item.Id, postId);
        FilterDefinition<GamePost> filter = Builders<GamePost>.Filter.And(playerFilter, postFilter);
        UpdateDefinition<GamePost> update = Builders<GamePost>.Update.Push(item => item.Comments, reply);
        UpdateResult result = await _posts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async ValueTask<StarConversation?> GetStarConversationAsync(
        PlayerId playerId,
        StarId starId,
        CancellationToken cancellationToken)
    {
        return await _starMemories
            .Find(item => item.PlayerId == playerId && item.StarId == starId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveStarConversationAsync(
        StarConversation conversation,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await _starMemories.ReplaceOneAsync(
            item => item.PlayerId == conversation.PlayerId && item.StarId == conversation.StarId,
            conversation,
            options,
            cancellationToken);
    }

    public async ValueTask<DailyGameEvent?> GetTodayEventAsync(
        PlayerId playerId,
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

        return await _events
            .Find(filter)
            .SortByDescending(item => item.DayStartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveEventAsync(
        DailyGameEvent gameEvent,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await _events.ReplaceOneAsync(
            item => item.Id == gameEvent.Id,
            gameEvent,
            options,
            cancellationToken);
    }

    public async ValueTask<GameChallenge?> GetActiveChallengeAsync(
        PlayerId playerId,
        CancellationToken cancellationToken)
    {
        return await _challenges.Find(item => item.PlayerId == playerId).FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveChallengeAsync(
        GameChallenge challenge,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await _challenges.ReplaceOneAsync(
            item => item.Id == challenge.Id,
            challenge,
            options,
            cancellationToken);
    }

    public async ValueTask<WorldRules?> GetWorldRulesAsync(
        PlayerId playerId,
        WorldId worldId,
        CancellationToken cancellationToken)
    {
        return await _worlds
            .Find(item => item.PlayerId == playerId && item.WorldId == worldId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveWorldRulesAsync(
        WorldRules rules,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await _worlds.ReplaceOneAsync(
            item => item.PlayerId == rules.PlayerId && item.WorldId == rules.WorldId,
            rules,
            options,
            cancellationToken);
    }

    public async ValueTask AppendAnalyticsAsync(
        AnalyticsBatch batch,
        CancellationToken cancellationToken)
    {
        await _analytics.InsertOneAsync(batch, cancellationToken: cancellationToken);
    }

    public async ValueTask AppendThreadAsync(
        PlayerId playerId,
        PostId postId,
        string hook,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        var document = new BsonDocument
        {
            ["PlayerId"] = playerId.Value,
            ["PostId"] = postId.Value,
            ["Hook"] = hook,
            ["CreatedAtUtc"] = createdAtUtc.UtcDateTime,
            ["Open"] = true,
        };
        await _threads.InsertOneAsync(document, cancellationToken: cancellationToken);
    }
}
