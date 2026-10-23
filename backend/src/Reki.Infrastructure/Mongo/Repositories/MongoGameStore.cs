using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application;
using Reki.Application.Posts;
using Reki.Domain.Events;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoGameStore : IGameStore
{
    private readonly IMongoClient _client;
    private readonly IMongoCollection<PlayerProfile> _players;
    private readonly IMongoCollection<GamePost> _posts;
    private readonly IMongoCollection<Publication> _publications;
    private readonly IMongoCollection<BsonDocument> _starMemories;
    private readonly IMongoCollection<DailyGameEvent> _events;
    private readonly IMongoCollection<BsonDocument> _challenges;
    private readonly IMongoCollection<World> _worlds;
    private readonly IMongoCollection<Invitation> _invites;

    public MongoGameStore(MongoCompositionRoot compositionRoot)
    {
        _client = compositionRoot.MongoClient;
        var database = compositionRoot.MongoDatabase;

        _players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        _posts = database.GetCollection<GamePost>(MongoCollections.Posts);
        _publications = database.GetCollection<Publication>(MongoCollections.Publications);
        _starMemories = database.GetCollection<BsonDocument>(MongoCollections.StarsMemory);
        _events = database.GetCollection<DailyGameEvent>(MongoCollections.Events);
        _challenges = database.GetCollection<BsonDocument>(MongoCollections.Challenges);
        _worlds = database.GetCollection<World>(MongoCollections.Worlds);
        _invites = database.GetCollection<Invitation>(MongoCollections.Invites);
    }

    public async ValueTask DeletePlayerAsync(
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
        await session.WithTransactionAsync(DeleteAsync, cancellationToken: cancellationToken);

        async Task<bool> DeleteAsync(IClientSessionHandle transactionSession, CancellationToken transactionCancellationToken)
        {
            var ownedWorldIds = await _worlds.Find(transactionSession, item => item.Player.PlayerId == player.Id)
                .Project(item => item.Id).ToListAsync(transactionCancellationToken);
            await _posts.DeleteManyAsync(
                transactionSession,
                Builders<GamePost>.Filter.In(item => item.WorldId, ownedWorldIds),
                cancellationToken: transactionCancellationToken);
            await _publications.DeleteManyAsync(
                transactionSession,
                item => item.PlayerId == player.Id,
                cancellationToken: transactionCancellationToken);
            await _starMemories.DeleteManyAsync(
                transactionSession,
                new BsonDocument("PlayerId", new BsonBinaryData(player.Id.Value, GuidRepresentation.Standard)),
                cancellationToken: transactionCancellationToken);
            await _events.DeleteManyAsync(
                transactionSession,
                item => item.PlayerId == player.Id,
                cancellationToken: transactionCancellationToken);
            var challengeFilter = Builders<BsonDocument>.Filter.Or(
                new BsonDocument("Participants", new BsonBinaryData(player.Id.Value, GuidRepresentation.Standard)),
                new BsonDocument("PlayerId", new BsonBinaryData(player.Id.Value, GuidRepresentation.Standard)));
            await _challenges.DeleteManyAsync(
                transactionSession,
                challengeFilter,
                cancellationToken: transactionCancellationToken);
            await _worlds.DeleteManyAsync(
                transactionSession,
                item => item.Player.PlayerId == player.Id,
                cancellationToken: transactionCancellationToken);
            var inviteFilter = Builders<Invitation>.Filter.Or(
                Builders<Invitation>.Filter.Eq(item => item.InviterId, player.Id),
                Builders<Invitation>.Filter.Eq(item => item.InviteeId, player.Id),
                new BsonDocument("PlayerId", new BsonBinaryData(player.Id.Value, GuidRepresentation.Standard)));
            await _invites.DeleteManyAsync(
                transactionSession,
                inviteFilter,
                cancellationToken: transactionCancellationToken);
            await _players.DeleteOneAsync(
                transactionSession,
                item => item.Id == player.Id,
                cancellationToken: transactionCancellationToken);
            return true;
        }
    }

    public async ValueTask<bool> TryCommitPostAsync(
        PlayerProfile updatedPlayer,
        int expectedPlayerVersion,
        World updatedWorld,
        int expectedWorldVersion,
        PublishedPost published,
        string publicationId,
        Invitation? updatedInvite,
        int inviteBonusMaxPerDay,
        CancellationToken cancellationToken)
    {
        var post = published.Post;
        using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(CommitAsync, cancellationToken: cancellationToken);

        async Task<bool> CommitAsync(IClientSessionHandle transactionSession, CancellationToken transactionCancellationToken)
        {
            var readyFilter = Builders<Publication>.Filter.And(
                Builders<Publication>.Filter.Eq(item => item.Id, publicationId),
                Builders<Publication>.Filter.Eq(item => item.PlayerId, updatedPlayer.Id),
                Builders<Publication>.Filter.Eq(item => item.Status, PublicationStatusType.ReadyForModeration));
            var complete = Builders<Publication>.Update
                .Set(item => item.Status, PublicationStatusType.Completed)
                .Set(item => item.Result, published)
                .Set(item => item.Text, string.Empty)
                .Set(item => item.Parts, []);
            var publicationResult = await _publications.UpdateOneAsync(
                transactionSession,
                readyFilter,
                complete,
                cancellationToken: transactionCancellationToken);
            if (publicationResult.ModifiedCount != 1)
            {
                await transactionSession.AbortTransactionAsync(transactionCancellationToken);
                return false;
            }

            if ((await _players.ReplaceOneAsync(transactionSession, item => item.Id == updatedPlayer.Id && item.Version == expectedPlayerVersion, updatedPlayer, cancellationToken: transactionCancellationToken)).MatchedCount != 1 ||
                (await _worlds.ReplaceOneAsync(transactionSession, item => item.Id == updatedWorld.Id && item.Player.PlayerId == updatedPlayer.Id && item.Version == expectedWorldVersion, updatedWorld, cancellationToken: transactionCancellationToken))
                .MatchedCount != 1)
            {
                await transactionSession.AbortTransactionAsync(transactionCancellationToken);
                return false;
            }

            if (updatedInvite is not null)
            {
                var start = updatedPlayer.PostsPeriodStartedAtUtc;
                var end = start.AddDays(1);
                var inviterUses = await _invites.CountDocumentsAsync(
                    transactionSession,
                    item => item.InviterId == updatedInvite.InviterId && item.InviterBonusUsed &&
                            item.InviterBonusUsedAtUtc >= start && item.InviterBonusUsedAtUtc < end,
                    cancellationToken: transactionCancellationToken);
                if (inviterUses < inviteBonusMaxPerDay)
                {
                    var inviterUpdate = Builders<PlayerProfile>.Update
                        .Set(item => item.HitOwnWorldMilestone, true)
                        .Inc(item => item.Energy, updatedInvite.BonusAmount)
                        .Inc(item => item.Version, 1);
                    await _players.UpdateOneAsync(
                        transactionSession,
                        item => item.Id == updatedInvite.InviterId,
                        inviterUpdate,
                        cancellationToken: transactionCancellationToken);
                }

                var inviteUpdate = Builders<Invitation>.Update
                    .Set(item => item.InviterBonusUsed, updatedInvite.InviterBonusUsed)
                    .Set(item => item.InviteeBonusUsed, updatedInvite.InviteeBonusUsed)
                    .Set(item => item.InviterBonusUsedAtUtc, updatedInvite.InviterBonusUsedAtUtc)
                    .Set(item => item.InviteeBonusUsedAtUtc, updatedInvite.InviteeBonusUsedAtUtc);
                var inviteResult = await _invites.UpdateOneAsync(
                    transactionSession,
                    item => item.Id == updatedInvite.Id && !item.InviteeBonusUsed,
                    inviteUpdate,
                    cancellationToken: transactionCancellationToken);
                if (inviteResult.ModifiedCount != 1)
                {
                    await transactionSession.AbortTransactionAsync(transactionCancellationToken);
                    return false;
                }
            }

            await _posts.InsertOneAsync(transactionSession, post, cancellationToken: transactionCancellationToken);
            return true;
        }
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
}