using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoProductGoalRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider) : IProductGoalRepository
{
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    private readonly IMongoCollection<BsonDocument> _goals =
        database.GetCollection<BsonDocument>(MongoCollections.ProductGoals);

    private readonly IMongoCollection<GamePost> _posts =
        database.GetCollection<GamePost>(MongoCollections.Posts);

    public async ValueTask EnsurePlayerAsync(PlayerId playerId, CancellationToken cancellationToken)
    {
        var id = playerId.Value.ToString("N");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        await _goals.UpdateOneAsync(
            filter,
            Builders<BsonDocument>.Update.SetOnInsert("referral_pending", false),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    public async ValueTask EnrollReferralAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _goals.InsertOneAsync(
            sessionProvider.Session,
            new BsonDocument { ["_id"] = playerId.Value.ToString("N"), ["referral_pending"] = true },
            cancellationToken: cancellationToken);

    public async ValueTask<IReadOnlyList<string>> RecordActivityAsync(
        PlayerId playerId,
        DateTimeOffset occurredAt,
        bool ordinaryPost,
        CancellationToken cancellationToken)
    {
        var session = sessionProvider.Session;
        var key = occurredAt.ToOffset(MoscowOffset).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", playerId.Value.ToString("N"));
        var state = await _goals.Find(session, filter).FirstAsync(cancellationToken);
        var days = state.GetValue("days", new BsonDocument()).AsBsonDocument;
        var firstActivity = !days.TryGetValue(key, out var day);
        var count = firstActivity
            ? await CountSavedPostsAsync(playerId, occurredAt, cancellationToken)
            : day.AsBsonDocument["posts"].AsInt64;
        var referralPending = state.GetValue("referral_pending", false).AsBoolean;
        if (!firstActivity && !referralPending && !ordinaryPost)
        {
            return [];
        }

        List<string> goals = [];
        if (firstActivity)
        {
            goals.Add("dau");
        }

        if (referralPending)
        {
            goals.Add("invited_friend_dau");
        }

        if (ordinaryPost)
        {
            count++;
            if (count == 1)
            {
                goals.Add("post_1_daily");
            }

            if (count == 3)
            {
                goals.Add("post_3_daily");
            }
        }

        var update = Builders<BsonDocument>.Update
            .Set($"days.{key}.posts", count)
            .Set("referral_pending", false);
        await _goals.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken);
        return goals;
    }

    public async ValueTask DeletePlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _goals.DeleteOneAsync(
            sessionProvider.Session,
            Builders<BsonDocument>.Filter.Eq("_id", playerId.Value.ToString("N")),
            cancellationToken: cancellationToken);

    private Task<long> CountSavedPostsAsync(
        PlayerId playerId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(occurredAt.ToOffset(MoscowOffset).Date, MoscowOffset).ToUniversalTime();
        var end = start.AddDays(1);
        var author = playerId.ToAuthorId();
        return _posts.CountDocumentsAsync(
            sessionProvider.Session,
            post => post.OwnerId == playerId && post.AuthorId == author && post.RepostId == null &&
                    post.CreatedAtUtc >= start && post.CreatedAtUtc < end,
            cancellationToken: cancellationToken);
    }
}
