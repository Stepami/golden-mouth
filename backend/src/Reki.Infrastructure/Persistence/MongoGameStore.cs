using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Common;
using Reki.Application.Posts;
using Reki.Contracts.Posts;
using Reki.Domain.Events;
using Reki.Domain.Invites;
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
    private readonly IMongoCollection<World> _worlds;
    private readonly IMongoCollection<World> _templates;
    private readonly IMongoCollection<BsonDocument> _threads;
    private readonly IMongoCollection<Invitation> _invites;

    public MongoGameStore(MongoCompositionRoot compositionRoot)
    {
        _client = compositionRoot.MongoClient;
        var database = compositionRoot.MongoDatabase;

        _players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        _posts = database.GetCollection<GamePost>(MongoCollections.Posts);
        _publications = database.GetCollection<Publication>(MongoCollections.Publications);
        _starMemories = database.GetCollection<StarConversation>(MongoCollections.StarsMemory);
        _events = database.GetCollection<DailyGameEvent>(MongoCollections.Events);
        _challenges = database.GetCollection<GameChallenge>(MongoCollections.Challenges);
        _worlds = database.GetCollection<World>(MongoCollections.Worlds);
        _templates = database.GetCollection<World>(MongoCollections.WorldTemplates);
        _threads = database.GetCollection<BsonDocument>(MongoCollections.Threads);
        _invites = database.GetCollection<Invitation>(MongoCollections.Invites);
    }

    public async ValueTask<long> CountPlayersAsync(CancellationToken cancellationToken) =>
        await _players.CountDocumentsAsync(FilterDefinition<PlayerProfile>.Empty, cancellationToken: cancellationToken);

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        TelegramUserId telegramUserId,
        CancellationToken cancellationToken) =>
        await _players.Find(item => item.TelegramUserId == telegramUserId).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _players.Find(item => item.Id == playerId).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask CreateInviteAsync(Invitation invite, CancellationToken cancellationToken) =>
        await _invites.InsertOneAsync(invite, cancellationToken: cancellationToken);

    public async ValueTask<Invitation?> GetInviteAsync(PlayerId inviteeId, CancellationToken cancellationToken) =>
        await _invites.Find(item => item.InviteeId == inviteeId).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<IReadOnlyList<Invitation>> GetInvitesByInviterAsync(
        PlayerId inviterId,
        CancellationToken cancellationToken) =>
        await _invites.Find(item => item.InviterId == inviterId).ToListAsync(cancellationToken);

    public async ValueTask CreatePlayerAsync(PlayerProfile player, CancellationToken cancellationToken) =>
        await _players.InsertOneAsync(player, cancellationToken: cancellationToken);

    public async ValueTask InsertWorldAsync(World world, CancellationToken cancellationToken) =>
        await _worlds.InsertOneAsync(world, cancellationToken: cancellationToken);

    public async ValueTask SavePlusNotificationAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken)
    {
        var filter = Builders<PlayerProfile>.Filter.And(
            Builders<PlayerProfile>.Filter.Eq(item => item.Id, player.Id),
            Builders<PlayerProfile>.Filter.Ne(item => item.PlusNotifyRequested, updated.PlusNotifyRequested));
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.PlusNotifyRequested, updated.PlusNotifyRequested)
            .Inc(item => item.Version, updated.Version - player.Version);
        await _players.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async ValueTask<bool> TryClaimPlusInterestAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken)
    {
        var filter = Builders<PlayerProfile>.Filter.And(
            Builders<PlayerProfile>.Filter.Eq(item => item.Id, player.Id),
            Builders<PlayerProfile>.Filter.Ne(item => item.PlusInterestClaimed, updated.PlusInterestClaimed));
        var update = Builders<PlayerProfile>.Update
            .Inc(item => item.DailyBonus, updated.DailyBonus - player.DailyBonus)
            .Set(item => item.PlusInterestClaimed, updated.PlusInterestClaimed)
            .Inc(item => item.Version, updated.Version - player.Version);
        var result = await _players.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async ValueTask<bool> TryReplacePlayerAsync(PlayerProfile player, int expectedVersion, CancellationToken cancellationToken)
    {
        var result = await _players.ReplaceOneAsync(item => item.Id == player.Id && item.Version == expectedVersion, player, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async ValueTask<bool> TryReplaceWorldAsync(World world, int expectedVersion, CancellationToken cancellationToken)
    {
        var result = await _worlds.ReplaceOneAsync(item => item.Id == world.Id && item.Player.PlayerId == world.Player.PlayerId && item.Version == expectedVersion, world, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async ValueTask DeletePlayerAsync(
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        var threadFilter = Builders<BsonDocument>.Filter
            .Eq("PlayerId", player.Id.Value);
        var ownedWorldIds = await _worlds.Find(item => item.Player.PlayerId == player.Id)
            .Project(item => item.Id).ToListAsync(cancellationToken);
        Task deletePosts = _posts.DeleteManyAsync(
            Builders<GamePost>.Filter.In(item => item.WorldId, ownedWorldIds),
            cancellationToken);
        Task deletePublications = _publications.DeleteManyAsync(
            item => item.PlayerId == player.Id,
            cancellationToken);
        Task deleteMemories = _starMemories.DeleteManyAsync(
            item => item.PlayerId == player.Id,
            cancellationToken);
        Task deleteEvents = _events.DeleteManyAsync(item => item.PlayerId == player.Id, cancellationToken);
        var challengeFilter = Builders<GameChallenge>.Filter
            .AnyEq(item => item.Participants, player.Id);
        Task deleteChallenges = _challenges.DeleteManyAsync(challengeFilter, cancellationToken);
        Task deleteWorlds = _worlds.DeleteManyAsync(item => item.Player.PlayerId == player.Id, cancellationToken);
        Task deleteThreads = _threads.DeleteManyAsync(threadFilter, cancellationToken);
        var inviteFilter = Builders<Invitation>.Filter.Or(
            Builders<Invitation>.Filter.Eq(item => item.InviterId, player.Id),
            Builders<Invitation>.Filter.Eq(item => item.InviteeId, player.Id),
            new BsonDocument("PlayerId", player.Id.Value));
        Task deleteInvites = _invites.DeleteManyAsync(inviteFilter, cancellationToken);

        await Task.WhenAll(
            deletePosts,
            deletePublications,
            deleteMemories,
            deleteEvents,
            deleteChallenges,
            deleteWorlds,
            deleteThreads,
            deleteInvites);
        await _players.DeleteOneAsync(item => item.Id == player.Id, cancellationToken);
    }

    public async ValueTask<bool> TryCommitPostAsync(
        PlayerProfile updatedPlayer,
        int expectedPlayerVersion,
        World updatedWorld,
        int expectedWorldVersion,
        PublishedPost published,
        string publicationId,
        Invitation? updatedInvite,
        CancellationToken cancellationToken)
    {
        var post = published.Post;
        using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();
        var readyFilter = Builders<Publication>.Filter.And(
            Builders<Publication>.Filter.Eq(item => item.Id, publicationId),
            Builders<Publication>.Filter.Eq(item => item.PlayerId, updatedPlayer.Id),
            Builders<Publication>.Filter.Eq(item => item.Status, PublicationStatus.ReadyForModeration));
        var complete = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatus.Completed)
            .Set(item => item.Result, published)
            .Set(item => item.Text, string.Empty)
            .Set(item => item.Parts, []);
        var publicationResult = await _publications.UpdateOneAsync(
            session,
            readyFilter,
            complete,
            cancellationToken: cancellationToken);
        if (publicationResult.ModifiedCount != 1)
        {
            await session.AbortTransactionAsync(cancellationToken);
            return false;
        }

        if ((await _players.ReplaceOneAsync(session, item => item.Id == updatedPlayer.Id && item.Version == expectedPlayerVersion, updatedPlayer, cancellationToken: cancellationToken)).MatchedCount != 1 ||
            (await _worlds.ReplaceOneAsync(session, item => item.Id == updatedWorld.Id && item.Player.PlayerId == updatedPlayer.Id && item.Version == expectedWorldVersion, updatedWorld, cancellationToken: cancellationToken))
            .MatchedCount != 1)
        {
            await session.AbortTransactionAsync(cancellationToken);
            return false;
        }

        if (updatedInvite is not null)
        {
            var inviteUpdate = Builders<Invitation>.Update
                .Set(item => item.InviterBonusUsed, updatedInvite.InviterBonusUsed)
                .Set(item => item.InviteeBonusUsed, updatedInvite.InviteeBonusUsed)
                .Set(item => item.InviterBonusUsedAtUtc, updatedInvite.InviterBonusUsedAtUtc)
                .Set(item => item.InviteeBonusUsedAtUtc, updatedInvite.InviteeBonusUsedAtUtc);
            var inviteResult = await _invites.UpdateOneAsync(
                session,
                item => item.Id == updatedInvite.Id && !item.InviteeBonusUsed,
                inviteUpdate,
                cancellationToken: cancellationToken);
            if (inviteResult.ModifiedCount != 1)
            {
                await session.AbortTransactionAsync(cancellationToken);
                return false;
            }
        }

        if (updatedInvite is { InviterBonusUsed: true })
        {
            await _players.UpdateOneAsync(
                session,
                item => item.Id == updatedInvite.InviterId,
                Builders<PlayerProfile>.Update.Set(item => item.HitOwnWorldMilestone, true).Inc(item => item.Version, 1),
                cancellationToken: cancellationToken);
        }

        await _posts.InsertOneAsync(session, post, cancellationToken: cancellationToken);
        await _threads.InsertOneAsync(
            session,
            CreateThreadDocument(updatedPlayer.Id, post.Id, updatedWorld.Id, published.Hook, post.CreatedAtUtc),
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
        var update = Builders<Publication>.Update.Push(item => item.Parts, part);
        var result = await _publications.UpdateOneAsync(
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
        var update = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatus.ReadyForModeration)
            .Set(item => item.ExpiresAtUtc, null);
        var result = await _publications.UpdateOneAsync(
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
        PublicationFailure failure,
        CancellationToken cancellationToken)
    {
        var update = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatus.Failed)
            .Set(item => item.Failure, failure)
            .Set(item => item.Text, string.Empty)
            .Set(item => item.Parts, [])
            .Set(item => item.ExpiresAtUtc, expiresAtUtc);
        await _publications.UpdateOneAsync(
            item => item.Id == id && item.Status != PublicationStatus.Completed,
            update,
            cancellationToken: cancellationToken);
    }

    public async ValueTask<IReadOnlyList<GamePost>> GetPostsAsync(
        PlayerId playerId,
        int? limit,
        CancellationToken cancellationToken,
        WorldId worldId)
    {
        if (await GetWorldAsync(playerId, worldId, cancellationToken) is null)
        {
            return [];
        }

        IFindFluent<GamePost, GamePost> query = _posts
            .Find(item => item.WorldId == worldId)
            .SortByDescending(item => item.CreatedAtUtc);
        if (limit is { } count)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
            query = query.Limit(count);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async ValueTask<GamePost?> GetPostAsync(
        PostId postId,
        PlayerId playerId,
        CancellationToken cancellationToken)
    {
        var post = await _posts.Find(item => item.Id == postId).FirstOrDefaultAsync(cancellationToken);
        return post is not null && await GetWorldAsync(playerId, post.WorldId, cancellationToken) is not null
            ? post
            : null;
    }

    public async ValueTask<bool> TryCommitReplyAsync(
        GamePost updatedPost,
        int expectedPostVersion,
        string requestId,
        PlayerProfile updatedPlayer,
        int expectedPlayerVersion,
        World updatedWorld,
        int expectedWorldVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            using var session = await _client.StartSessionAsync(
                cancellationToken: cancellationToken);
            session.StartTransaction();
            var requestIdField = new StringFieldDefinition<GamePost, string>("Comments.RequestId");
            var postFilter = Builders<GamePost>.Filter.And(
                Builders<GamePost>.Filter.Eq(item => item.Id, updatedPost.Id),
                Builders<GamePost>.Filter.Eq(item => item.WorldId, updatedWorld.Id),
                Builders<GamePost>.Filter.Eq(item => item.Version, expectedPostVersion),
                Builders<GamePost>.Filter.Ne(requestIdField, requestId));
            var postResult = await _posts.ReplaceOneAsync(
                session,
                postFilter,
                updatedPost,
                cancellationToken: cancellationToken);
            if (postResult.MatchedCount != 1)
            {
                await session.AbortTransactionAsync(cancellationToken);
                return false;
            }

            if ((await _players.ReplaceOneAsync(session, item => item.Id == updatedPlayer.Id && item.Version == expectedPlayerVersion, updatedPlayer, cancellationToken: cancellationToken)).MatchedCount != 1 ||
                (await _worlds.ReplaceOneAsync(session, item => item.Id == updatedWorld.Id && item.Player.PlayerId == updatedPlayer.Id && item.Version == expectedWorldVersion, updatedWorld, cancellationToken: cancellationToken))
                .MatchedCount != 1)
            {
                await session.AbortTransactionAsync(cancellationToken);
                return false;
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
        CancellationToken cancellationToken,
        WorldId worldId)
    {
        return await _starMemories
            .Find(item => item.PlayerId == playerId && item.StarId == starId && item.WorldId == worldId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveStarConversationAsync(
        StarConversation conversation,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await _starMemories.ReplaceOneAsync(
            item => item.PlayerId == conversation.PlayerId && item.WorldId == conversation.WorldId &&
                    item.StarId == conversation.StarId,
            conversation,
            options,
            cancellationToken);
    }

    public async ValueTask<DailyGameEvent?> GetTodayEventAsync(
        PlayerId playerId,
        DateTimeOffset dayStartedAtUtc,
        CancellationToken cancellationToken,
        WorldId worldId)
    {
        var filter = Builders<DailyGameEvent>.Filter
                         .Eq(item => item.PlayerId, playerId)
                     & Builders<DailyGameEvent>.Filter.Eq(item => item.WorldId, worldId);

        if (dayStartedAtUtc != DateTimeOffset.MinValue)
        {
            var dayFilter = Builders<DailyGameEvent>.Filter
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
        var participantFilter = Builders<GameChallenge>.Filter
            .AnyEq(item => item.Participants, playerId);
        var statusFilter = Builders<GameChallenge>.Filter
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
        var participantsFilter = Builders<GameChallenge>.Filter.And(
            Builders<GameChallenge>.Filter.AnyEq(item => item.Participants, firstPlayerId),
            Builders<GameChallenge>.Filter.AnyEq(item => item.Participants, secondPlayerId));
        var statusFilter = Builders<GameChallenge>.Filter
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
        var idFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Id, challenge.Id);
        var versionFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Version, expectedVersion);
        try
        {
            var result = await _challenges.ReplaceOneAsync(
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

    public async ValueTask<World?> GetWorldAsync(PlayerId playerId, WorldId worldId, CancellationToken cancellationToken)
    {
        return await _worlds.Find(item => item.Player.PlayerId == playerId && item.Id == worldId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<World>> GetWorldTemplatesAsync(CancellationToken cancellationToken)
    {
        return await _templates.Find(FilterDefinition<World>.Empty).ToListAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken)
    {
        return await _worlds.Find(item => item.Player.PlayerId == playerId).ToListAsync(cancellationToken);
    }

    private static BsonDocument CreateThreadDocument(
        PlayerId playerId,
        PostId postId,
        WorldId worldId,
        string hook,
        DateTimeOffset createdAtUtc) =>
        new()
        {
            ["PlayerId"] = playerId.Value,
            ["PostId"] = postId.Value,
            ["WorldId"] = worldId.Value,
            ["Hook"] = hook,
            ["CreatedAtUtc"] = createdAtUtc.UtcDateTime,
            ["Open"] = true,
        };
}
