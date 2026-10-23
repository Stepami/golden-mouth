using System.Text;
using System.Text.Json.Nodes;
using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reki.Domain.Posts;
using Reki.Infrastructure.Jobs.GenerateStarFeedPost;
using Reki.Infrastructure.Mongo.Repositories;
using TickerQ.Utilities;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class UseVersion7PostIds : RekiMigrationBase
{
    private const string Mappings = "migration_2_4_6_post_ids";
    private const string TimeTickers = "ticker_TimeTickers";

    public override DatabaseVersion Version => new(2, 4, 6);

    public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
        RewriteAsync(new MigrationContext(db, session, false, cancellationToken));

    public override async Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        await RewriteAsync(new MigrationContext(db, session, true, cancellationToken));
        await db.GetCollection<BsonDocument>(Mappings).DeleteManyAsync(session, FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
    }

    private static async Task RewriteAsync(MigrationContext context)
    {
        // RekiMigrator runs one migration in one transaction, including the rollback map.
        await RewriteDocumentsAsync(context, MongoCollections.Posts, false);
        await RewriteDocumentsAsync(context, MongoCollections.Publications, true);
        await RewriteInvitationsAsync(context);
        await RewriteGuidCollectionsAsync(context);
        await RewriteTickersAsync(context);
    }

    private static async Task RewriteGuidCollectionsAsync(MigrationContext context)
    {
        foreach (var (name, idKind) in new (string, string?)[]
        {
            (MongoCollections.Players, "Player"), (MongoCollections.Events, "Event"),
            (MongoCollections.Invites, null), (MongoCollections.Worlds, null),
            (MongoCollections.WorldTemplates, null), (MongoCollections.Publications, null),
            (MongoCollections.StarsMemory, null), (MongoCollections.Challenges, null),
        })
        {
            var collection = context.Database.GetCollection<BsonDocument>(name);
            using var cursor = await collection.FindAsync(context.Session, FilterDefinition<BsonDocument>.Empty, cancellationToken: context.CancellationToken);
            await cursor.ForEachAsync(
                async document =>
            {
                var original = document.DeepClone().AsBsonDocument;
                if (idKind is not null)
                {
                    document["_id"] = await ResolveIdAsync(context, document["_id"], idKind, document, "CreatedAtUtc");
                }

                await RewritePlayerReferencesAsync(context, document);
                if (document == original)
                {
                    return;
                }

                var identity = new BsonDocument("_id", original["_id"]);
                if (document["_id"] != original["_id"])
                {
                    await collection.DeleteOneAsync(context.Session, identity, cancellationToken: context.CancellationToken);
                    await collection.InsertOneAsync(context.Session, document, cancellationToken: context.CancellationToken);
                }
                else
                {
                    await collection.ReplaceOneAsync(context.Session, identity, document, cancellationToken: context.CancellationToken);
                }
            },
                context.CancellationToken);
        }
    }

    private static async Task RewritePlayerReferencesAsync(MigrationContext context, BsonDocument document)
    {
        foreach (var element in document.Elements.ToArray())
        {
            if (element.Name is "PlayerId" or "InviterId" or "InviteeId" && !element.Value.IsBsonNull)
            {
                document[element.Name] = await ResolveIdAsync(context, element.Value, "Player", document, "CreatedAtUtc");
            }
            else if (element.Name == "Participants" && element.Value.IsBsonArray)
            {
                await RewriteParticipantsAsync(context, element.Value.AsBsonArray, document);
            }
            else
            {
                if (element.Value.IsBsonDocument)
                {
                    await RewritePlayerReferencesAsync(context, element.Value.AsBsonDocument);
                }
            }
        }
    }

    private static async Task RewriteParticipantsAsync(MigrationContext context, BsonArray participants, BsonDocument source)
    {
        for (var index = 0; index < participants.Count; index++)
        {
            participants[index] = await ResolveIdAsync(context, participants[index], "Player", source, "CreatedAtUtc");
        }
    }

    private static async Task RewriteDocumentsAsync(MigrationContext context, string collectionName, bool embedded)
    {
        var collection = context.Database.GetCollection<BsonDocument>(collectionName);
        var filter = embedded
            ? new BsonDocument("Result.Post", new BsonDocument("$type", "object"))
            : FilterDefinition<BsonDocument>.Empty;
        using var cursor = await collection.FindAsync(context.Session, filter, cancellationToken: context.CancellationToken);
        await cursor.ForEachAsync(
            document => RewritePostAsync(context, collection, document, embedded),
            context.CancellationToken);
    }

    private static async Task RewritePostAsync(MigrationContext context, IMongoCollection<BsonDocument> collection, BsonDocument document, bool embedded)
    {
        var identity = new BsonDocument("_id", document["_id"]);
        var post = embedded ? document["Result"]["Post"].AsBsonDocument : document;
        var original = post.DeepClone().AsBsonDocument;
        post["_id"] = await ResolveIdAsync(context, post["_id"], "Post", post, "CreatedAtUtc");
        await RewriteCommentsAsync(context, post);
        if (post == original)
        {
            return;
        }

        if (embedded)
        {
            await collection.UpdateOneAsync(context.Session, identity, Builders<BsonDocument>.Update.Set("Result.Post", post), cancellationToken: context.CancellationToken);
        }
        else if (post["_id"] != original["_id"])
        {
            // MongoDB _id is immutable; retain every other BSON field verbatim.
            await collection.DeleteOneAsync(context.Session, identity, cancellationToken: context.CancellationToken);
            await collection.InsertOneAsync(context.Session, document, cancellationToken: context.CancellationToken);
        }
        else
        {
            await collection.ReplaceOneAsync(context.Session, identity, document, cancellationToken: context.CancellationToken);
        }
    }

    private static async Task RewriteCommentsAsync(MigrationContext context, BsonDocument post)
    {
        foreach (var comment in post.GetValue("Comments", new BsonArray()).AsBsonArray.Select(entry => entry.AsBsonDocument))
        {
            comment["_id"] = await ResolveIdAsync(context, comment["_id"], "Comment", post, "CreatedAtUtc");
            if (comment.TryGetValue("ParentCommentId", out var parent) && !parent.IsBsonNull)
            {
                comment["ParentCommentId"] = await ResolveIdAsync(context, parent, "Comment", post, "CreatedAtUtc");
            }
        }
    }

    private static async Task RewriteInvitationsAsync(MigrationContext context)
    {
        var collection = context.Database.GetCollection<BsonDocument>(MongoCollections.Invites);
        using var cursor = await collection.FindAsync(context.Session, FilterDefinition<BsonDocument>.Empty, cancellationToken: context.CancellationToken);
        await cursor.ForEachAsync(
            async document =>
            {
                var previousId = document["_id"];
                var nextId = await ResolveIdAsync(context, previousId, "Invitation", document, "CreatedAtUtc");
                if (nextId == previousId)
                {
                    return;
                }

                await collection.DeleteOneAsync(context.Session, new BsonDocument("_id", previousId), cancellationToken: context.CancellationToken);
                document["_id"] = nextId;
                await collection.InsertOneAsync(context.Session, document, cancellationToken: context.CancellationToken);
            },
            context.CancellationToken);
    }

    private static async Task RewriteTickersAsync(MigrationContext context)
    {
        var collection = context.Database.GetCollection<BsonDocument>(TimeTickers);
        using var cursor = await collection.FindAsync(context.Session, new BsonDocument("Function", nameof(GenerateStarFeedPostJob)), cancellationToken: context.CancellationToken);
        await cursor.ForEachAsync(ticker => RewriteTickerAsync(context, collection, ticker), context.CancellationToken);
    }

    private static async Task RewriteTickerAsync(MigrationContext context, IMongoCollection<BsonDocument> collection, BsonDocument ticker)
    {
        // Legacy ObjectIds must be migrated before the request can be read as ValueObject<Guid>.
        var request = JsonNode.Parse(TickerHelper.ReadTickerRequestAsString(ticker["Request"].AsBsonBinaryData.Bytes))?.AsObject()
            ?? throw new InvalidOperationException("The star feed request is missing.");
        var previousText = request["postId"]?.GetValue<string>()
            ?? throw new InvalidOperationException("The star feed post identifier is missing.");
        BsonValue previousId = context.Downgrade || Guid.TryParseExact(previousText, "D", out _)
            ? new BsonBinaryData(Guid.Parse(previousText), GuidRepresentation.Standard) : new BsonString(previousText);
        var nextId = await ResolveIdAsync(context, previousId, "Post", ticker, "CreatedAt");
        request["postId"] = nextId.IsBsonBinaryData ? nextId.AsBsonBinaryData.ToGuid(GuidRepresentation.Standard).ToString("D") : nextId.AsString;
        var playerText = request["playerId"]?.GetValue<string>() ?? throw new InvalidOperationException("The star feed player identifier is missing.");
        BsonValue playerId = context.Downgrade || Guid.TryParseExact(playerText, "D", out _)
            ? new BsonBinaryData(Guid.Parse(playerText), GuidRepresentation.Standard) : new BsonString(playerText);
        var nextPlayerId = await ResolveIdAsync(context, playerId, "Player", ticker, "CreatedAt");
        request["playerId"] = nextPlayerId.IsBsonBinaryData ? nextPlayerId.AsBsonBinaryData.ToGuid(GuidRepresentation.Standard).ToString("D") : nextPlayerId.AsString;
        var payload = TickerHelper.CreateTickerRequest(Encoding.UTF8.GetBytes(request.ToJsonString()));
        await collection.UpdateOneAsync(context.Session, new BsonDocument("_id", ticker["_id"]), Builders<BsonDocument>.Update.Set("Request", new BsonBinaryData(payload)), cancellationToken: context.CancellationToken);
    }

    private static async Task<BsonValue> ResolveIdAsync(MigrationContext context, BsonValue previousId, string kind, BsonDocument source, string timestampField)
    {
        var mappings = context.Database.GetCollection<BsonDocument>(Mappings);
        var key = new BsonDocument { ["Kind"] = kind, ["OldId"] = previousId };
        var filter = context.Downgrade ? new BsonDocument { ["_id.Kind"] = kind, ["NewId"] = previousId } : new BsonDocument("_id", key);
        var existing = await mappings.Find(context.Session, filter).FirstOrDefaultAsync(context.CancellationToken);
        if (existing is not null)
        {
            return context.Downgrade ? existing["_id"]["OldId"] : existing["NewId"];
        }

        if (context.Downgrade || IsCurrentId(previousId))
        {
            return previousId;
        }

        var parsed = previousId.IsString && Guid.TryParse(previousId.AsString, out var guid) ? guid : (Guid?)null;
        var fallback = kind is "Comment" or "Invitation" ? Guid.NewGuid() : parsed;
        var value = kind == "Post" && parsed?.Version != 7
            ? PostId.New(ReadTimestamp(source[timestampField])).Value
            : parsed ?? fallback ?? Guid.Parse(previousId.AsString);
        var nextId = new BsonBinaryData(value, GuidRepresentation.Standard);
        await mappings.InsertOneAsync(context.Session, new BsonDocument { ["_id"] = key, ["NewId"] = nextId }, cancellationToken: context.CancellationToken);
        return nextId;
    }

    private static bool IsCurrentId(BsonValue value) =>
        value.IsBsonBinaryData && value.AsBsonBinaryData.SubType == BsonBinarySubType.UuidStandard;

    private static DateTimeOffset ReadTimestamp(BsonValue value)
    {
        if (value.IsBsonDateTime)
        {
            return new DateTimeOffset(value.ToUniversalTime());
        }

        using var reader = new BsonDocumentReader(new BsonDocument("value", value));
        reader.ReadStartDocument();
        _ = reader.ReadName();
        return BsonSerializer.LookupSerializer<DateTimeOffset>().Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }

    private sealed record MigrationContext(IMongoDatabase Database, IClientSessionHandle Session, bool Downgrade, CancellationToken CancellationToken);
}
