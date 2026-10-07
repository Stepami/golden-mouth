using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Common;
using Reki.Application.Posts;
using Reki.Contracts.Posts;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Persistence;

internal sealed class MongoGameStore : IGameStore
{
    private readonly IMongoClient _client;
    private readonly IMongoCollection<PlayerProfile> _players;
    private readonly IMongoCollection<GamePost> _posts;
    private readonly IMongoCollection<Publication> _publications;
    private readonly IMongoCollection<StarConversation> _starMemories;
    private readonly IMongoCollection<DailyGameEvent> _events;
    private readonly IMongoCollection<GameChallenge> _challenges;
    private readonly IMongoCollection<WorldRules> _worlds;
    private readonly IMongoCollection<BsonDocument> _threads;
    private readonly IMongoCollection<BsonDocument> _invites;

    public MongoGameStore(IMongoClient client, IMongoDatabase database)
    {
        _client = client;
        _players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        _posts = database.GetCollection<GamePost>(MongoCollections.Posts);
        _publications = database.GetCollection<Publication>(MongoCollections.Publications);
        _starMemories = database.GetCollection<StarConversation>(MongoCollections.StarsMemory);
        _events = database.GetCollection<DailyGameEvent>(MongoCollections.Events);
        _challenges = database.GetCollection<GameChallenge>(MongoCollections.Challenges);
        _worlds = database.GetCollection<WorldRules>(MongoCollections.Worlds);
        _threads = database.GetCollection<BsonDocument>(MongoCollections.Threads);
        _invites = database.GetCollection<BsonDocument>(MongoCollections.Invites);
    }

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        TelegramUserId telegramUserId,
        CancellationToken cancellationToken)
    {
        return await _players
            .Find(player => player.TelegramUserId == telegramUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        PlayerId playerId,
        CancellationToken cancellationToken)
    {
        return await _players
            .Find(player => player.Id == playerId)
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
        Task deletePlayer = _players.DeleteOneAsync(
            item => item.TelegramUserId == player.TelegramUserId,
            cancellationToken);
        Task deletePosts = _posts.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deletePublications = _publications.DeleteManyAsync(
            item => item.PlayerId == player.Id,
            cancellationToken);
        Task deleteMemories = _starMemories.DeleteManyAsync(
            item => item.PlayerId == player.Id,
            cancellationToken);
        Task deleteEvents = _events.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        FilterDefinition<GameChallenge> challengeFilter = Builders<GameChallenge>.Filter
            .AnyEq(item => item.Participants, player.Id);
        Task deleteChallenges = _challenges.DeleteManyAsync(challengeFilter, cancellationToken);
        Task deleteWorlds = _worlds.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        Task deleteThreads = _threads.DeleteManyAsync(threadFilter, cancellationToken);
        Task deleteInvites = _invites.DeleteManyAsync(threadFilter, cancellationToken);

        await Task.WhenAll(
            deletePlayer,
            deletePosts,
            deletePublications,
            deleteMemories,
            deleteEvents,
            deleteChallenges,
            deleteWorlds,
            deleteThreads,
            deleteInvites);
    }

    public async ValueTask AddPostAsync(GamePost post, CancellationToken cancellationToken)
    {
        await _posts.InsertOneAsync(post, cancellationToken: cancellationToken);
    }

    public async ValueTask<bool> TryCommitPostAsync(
        PlayerProfile updatedPlayer,
        int expectedPlayerVersion,
        PublishedPost published,
        string publicationId,
        CancellationToken cancellationToken)
    {
        GamePost post = published.Post;
        using IClientSessionHandle session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();
        FilterDefinition<Publication> readyFilter = Builders<Publication>.Filter.And(
            Builders<Publication>.Filter.Eq(item => item.Id, publicationId),
            Builders<Publication>.Filter.Eq(item => item.PlayerId, updatedPlayer.Id),
            Builders<Publication>.Filter.Eq(item => item.Status, PublicationStatus.ReadyForModeration));
        UpdateDefinition<Publication> complete = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatus.Completed)
            .Set(item => item.Result, published)
            .Set(item => item.Text, string.Empty)
            .Set(item => item.Parts, []);
        UpdateResult publicationResult = await _publications.UpdateOneAsync(
            session,
            readyFilter,
            complete,
            cancellationToken: cancellationToken);
        if (publicationResult.ModifiedCount != 1)
        {
            await session.AbortTransactionAsync(cancellationToken);
            return false;
        }

        FilterDefinition<PlayerProfile> playerFilter = Builders<PlayerProfile>.Filter.And(
            Builders<PlayerProfile>.Filter.Eq(item => item.Id, updatedPlayer.Id),
            Builders<PlayerProfile>.Filter.Eq(item => item.Version, expectedPlayerVersion));
        ReplaceOneResult replaced = await _players.ReplaceOneAsync(
            session,
            playerFilter,
            updatedPlayer,
            cancellationToken: cancellationToken);
        if (replaced.ModifiedCount != 1)
        {
            await session.AbortTransactionAsync(cancellationToken);
            return false;
        }

        await _posts.InsertOneAsync(session, post, cancellationToken: cancellationToken);
        await _threads.InsertOneAsync(
            session,
            CreateThreadDocument(updatedPlayer.Id, post.Id, published.Hook, post.CreatedAtUtc),
            cancellationToken: cancellationToken);
        await session.CommitTransactionAsync(cancellationToken);
        return true;
    }

    public async ValueTask<Publication?> GetPublicationAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return await _publications.Find(item => item.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<bool> TryCreatePublicationAsync(
        Publication publication,
        CancellationToken cancellationToken)
    {
        try
        {
            await _publications.InsertOneAsync(publication, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async ValueTask AppendPublicationPartAsync(
        string id,
        PublicationPart part,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<Publication> update = Builders<Publication>.Update.Push(item => item.Parts, part);
        UpdateResult result = await _publications.UpdateOneAsync(
            item => item.Id == id && item.Status == PublicationStatus.Pending,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount != 1)
        {
            throw new InvalidOperationException("The publication is no longer pending.");
        }
    }

    public async ValueTask MarkPublicationReadyAsync(string id, CancellationToken cancellationToken)
    {
        UpdateDefinition<Publication> update = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatus.ReadyForModeration)
            .Set(item => item.ExpiresAtUtc, null);
        UpdateResult result = await _publications.UpdateOneAsync(
            item => item.Id == id && item.Status == PublicationStatus.Pending,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount != 1)
        {
            throw new InvalidOperationException("The publication could not be marked ready.");
        }
    }

    public async ValueTask MarkPublicationFailedAsync(
        string id,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<Publication> update = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatus.Failed)
            .Set(item => item.Text, string.Empty)
            .Set(item => item.Parts, [])
            .Set(item => item.ExpiresAtUtc, expiresAtUtc);
        await _publications.UpdateOneAsync(
            item => item.Id == id && item.Status != PublicationStatus.Completed,
            update,
            cancellationToken: cancellationToken);
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

    public async ValueTask<IReadOnlyList<GamePost>> GetRecentWorldPostsAsync(
        WorldId worldId,
        int count,
        CancellationToken cancellationToken)
    {
        List<PlayerId> playerIds = await _players
            .Find(item => item.World == worldId)
            .Project(item => item.Id)
            .ToListAsync(cancellationToken);
        if (playerIds.Count == 0)
        {
            return [];
        }

        return await _posts
            .Find(Builders<GamePost>.Filter.In(item => item.PlayerId, playerIds))
            .SortByDescending(item => item.CreatedAtUtc)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<PlayerProfile>> GetPlayersAsync(
        IReadOnlyCollection<PlayerId> playerIds,
        CancellationToken cancellationToken)
    {
        if (playerIds.Count == 0)
        {
            return [];
        }

        return await _players
            .Find(Builders<PlayerProfile>.Filter.In(item => item.Id, playerIds))
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<long>> GetRecentPostViewsAsync(
        PlayerId playerId,
        int count,
        CancellationToken cancellationToken)
    {
        return await _posts
            .Find(item => item.PlayerId == playerId)
            .SortByDescending(item => item.CreatedAtUtc)
            .Limit(count)
            .Project(item => item.Views)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<GamePost?> GetPostAsync(
        PostId postId,
        CancellationToken cancellationToken)
    {
        return await _posts
            .Find(item => item.Id == postId)
            .FirstOrDefaultAsync(cancellationToken);
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

    public async ValueTask<bool> TryAddThreadReplyAsync(
        PlayerId playerId,
        PostId postId,
        ThreadReply reply,
        GameComment comment,
        PlayerProfile? updatedPlayer,
        int expectedPlayerVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            using IClientSessionHandle session = await _client.StartSessionAsync(
                cancellationToken: cancellationToken);
            session.StartTransaction();
            var replyIdField = new StringFieldDefinition<GamePost, string>("ThreadReplies.ReplyId");
            FilterDefinition<GamePost> postFilter = Builders<GamePost>.Filter.And(
                Builders<GamePost>.Filter.Eq(item => item.Id, postId),
                Builders<GamePost>.Filter.Eq(item => item.PlayerId, playerId),
                Builders<GamePost>.Filter.Ne(replyIdField, reply.ReplyId));
            UpdateDefinition<GamePost> update = Builders<GamePost>.Update
                .Push(item => item.Comments, comment)
                .Push(item => item.ThreadReplies, reply);
            UpdateResult postResult = await _posts.UpdateOneAsync(
                session,
                postFilter,
                update,
                cancellationToken: cancellationToken);
            if (postResult.ModifiedCount != 1)
            {
                await session.AbortTransactionAsync(cancellationToken);
                return false;
            }

            if (updatedPlayer is not null)
            {
                FilterDefinition<PlayerProfile> playerFilter = Builders<PlayerProfile>.Filter.And(
                    Builders<PlayerProfile>.Filter.Eq(item => item.Id, updatedPlayer.Id),
                    Builders<PlayerProfile>.Filter.Eq(item => item.Version, expectedPlayerVersion));
                ReplaceOneResult replaced = await _players.ReplaceOneAsync(
                    session,
                    playerFilter,
                    updatedPlayer,
                    cancellationToken: cancellationToken);
                if (replaced.ModifiedCount != 1)
                {
                    await session.AbortTransactionAsync(cancellationToken);
                    return false;
                }
            }

            await session.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch (MongoException exception) when (exception.HasErrorLabel("TransientTransactionError"))
        {
            return false;
        }
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
        FilterDefinition<GameChallenge> participantFilter = Builders<GameChallenge>.Filter
            .AnyEq(item => item.Participants, playerId);
        FilterDefinition<GameChallenge> statusFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Status, ChallengeStatus.Active);
        return await _challenges
            .Find(Builders<GameChallenge>.Filter.And(participantFilter, statusFilter))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<GameChallenge?> GetPendingChallengeAsync(
        PlayerId firstPlayerId,
        PlayerId secondPlayerId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<GameChallenge> participantsFilter = Builders<GameChallenge>.Filter.And(
            Builders<GameChallenge>.Filter.AnyEq(item => item.Participants, firstPlayerId),
            Builders<GameChallenge>.Filter.AnyEq(item => item.Participants, secondPlayerId));
        FilterDefinition<GameChallenge> statusFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Status, ChallengeStatus.Pending);
        return await _challenges
            .Find(Builders<GameChallenge>.Filter.And(participantsFilter, statusFilter))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<bool> TryInsertChallengeAsync(
        GameChallenge challenge,
        CancellationToken cancellationToken)
    {
        try
        {
            await _challenges.InsertOneAsync(challenge, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (
            exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async ValueTask<bool> TryReplaceChallengeAsync(
        GameChallenge challenge,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        FilterDefinition<GameChallenge> idFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Id, challenge.Id);
        FilterDefinition<GameChallenge> versionFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Version, expectedVersion);
        try
        {
            ReplaceOneResult result = await _challenges.ReplaceOneAsync(
                Builders<GameChallenge>.Filter.And(idFilter, versionFilter),
                challenge,
                cancellationToken: cancellationToken);
            return result.ModifiedCount == 1;
        }
        catch (MongoWriteException exception) when (
            exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
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

    public async ValueTask AppendThreadAsync(
        PlayerId playerId,
        PostId postId,
        string hook,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        await _threads.InsertOneAsync(
            CreateThreadDocument(playerId, postId, hook, createdAtUtc),
            cancellationToken: cancellationToken);
    }

    private static BsonDocument CreateThreadDocument(
        PlayerId playerId,
        PostId postId,
        string hook,
        DateTimeOffset createdAtUtc) =>
        new()
        {
            ["PlayerId"] = playerId.Value,
            ["PostId"] = postId.Value,
            ["Hook"] = hook,
            ["CreatedAtUtc"] = createdAtUtc.UtcDateTime,
            ["Open"] = true,
        };
}
