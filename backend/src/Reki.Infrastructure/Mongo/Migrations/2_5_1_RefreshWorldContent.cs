using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed partial class RefreshWorldContent(ILogger<RefreshWorldContent> logger) : RekiMigrationBase
{
    private const string PersonaMarker = "\nПерсона игрока: ";

    public override DatabaseVersion Version => new(2, 5, 1);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        foreach (var collectionName in new[] { MongoCollections.WorldTemplates, MongoCollections.Worlds })
        {
            await MigrateCollectionAsync(db.GetCollection<BsonDocument>(collectionName), session, cancellationToken);
        }
    }

    private static async Task<string> MigrateDocumentAsync(IMongoCollection<BsonDocument> collection, IClientSessionHandle session, ContentRevision revision, BsonDocument document, CancellationToken cancellationToken)
    {
        if (Matches(document, revision.After, revision.StarsAfter))
        {
            return "AlreadyCurrent";
        }

        if (!Matches(document, revision.Before, revision.StarsBefore))
        {
            return "UnknownOrEditedContent";
        }

        await collection.UpdateOneAsync(
            session,
            new BsonDocument("_id", document["_id"]),
            new BsonDocument("$set", CreateUpdate(document, revision)),
            cancellationToken: cancellationToken);
        return "Updated";
    }

    private static bool Matches(BsonDocument document, BsonDocument fields, BsonDocument stars)
    {
        foreach (var field in fields)
        {
            if (!TryReadField(document, field.Name, out var actual))
            {
                return false;
            }

            if (field.Name == "Interface.Description" && actual.IsString)
            {
                var text = actual.AsString;
                var marker = text.IndexOf(PersonaMarker, StringComparison.Ordinal);
                actual = marker < 0 ? text : text[..marker];
            }

            if (actual != field.Value)
            {
                return false;
            }
        }

        if (!document.TryGetValue("Stars", out var value) || !value.IsBsonArray)
        {
            return false;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in value.AsBsonArray)
        {
            if (item is not BsonDocument star || !star.TryGetValue("_id", out var id) || !id.IsString ||
                !seen.Add(id.AsString) || !stars.TryGetValue(id.AsString, out var expected) || expected is not BsonDocument known)
            {
                return false;
            }

            if (known.Any(field => !star.TryGetValue(field.Name, out var actual) || actual != field.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryReadField(BsonDocument document, string path, out BsonValue value)
    {
        value = document;
        foreach (var name in path.Split('.'))
        {
            if (value is not BsonDocument parent || !parent.TryGetValue(name, out value))
            {
                value = BsonNull.Value;
                return false;
            }
        }

        return true;
    }

    private static BsonDocument CreateUpdate(BsonDocument document, ContentRevision revision)
    {
        var fields = revision.After.DeepClone().AsBsonDocument;
        var description = document["Interface"]["Description"].AsString;
        var marker = description.IndexOf(PersonaMarker, StringComparison.Ordinal);
        if (marker >= 0)
        {
            fields["Interface.Description"] = fields["Interface.Description"].AsString + description[marker..];
        }

        var stars = document["Stars"].AsBsonArray;
        for (var index = 0; index < stars.Count; index++)
        {
            var target = revision.StarsAfter[stars[index]["_id"].AsString].AsBsonDocument;
            AddStarFields(fields, target, index);
        }

        return fields;
    }

    private static void AddStarFields(BsonDocument fields, BsonDocument target, int index)
    {
        foreach (var field in target.Where(field => field.Name != "Name"))
        {
            fields[$"Stars.{index}.{field.Name}"] = field.Value.DeepClone();
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "World content migration attempt: {Collection}, template {TemplateId}, {Result}: {Count}")]
    private static partial void LogContentAttempt(ILogger logger, string collection, Guid templateId, string result, int count);

    private async Task MigrateCollectionAsync(IMongoCollection<BsonDocument> collection, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        foreach (var revision in CreateRevisions())
        {
            await MigrateRevisionAsync(collection, session, revision, cancellationToken);
        }
    }

    private async Task MigrateRevisionAsync(IMongoCollection<BsonDocument> collection, IClientSessionHandle session, ContentRevision revision, CancellationToken cancellationToken)
    {
        var emptyOwner = new BsonBinaryData(Guid.Empty, GuidRepresentation.Standard);
        var templateId = new BsonBinaryData(revision.TemplateId, GuidRepresentation.Standard);
        var filter = new BsonDocument("IsArchived", false);
        if (collection.CollectionNamespace.CollectionName == MongoCollections.WorldTemplates)
        {
            filter.Add("_id", templateId);
            filter.Add("Player.PlayerId", emptyOwner);
            filter.Add("TemplateId", BsonNull.Value);
        }
        else
        {
            filter.Add("TemplateId", templateId);
            filter.Add("Player.PlayerId", new BsonDocument("$ne", emptyOwner));
        }

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        using var cursor = await collection.FindAsync(session, filter, cancellationToken: cancellationToken);
        await cursor.ForEachAsync(
            async document =>
            {
                var result = await MigrateDocumentAsync(collection, session, revision, document, cancellationToken);
                counts[result] = counts.GetValueOrDefault(result) + 1;
            },
            cancellationToken);
        foreach (var (result, count) in counts)
        {
            LogContentAttempt(logger, collection.CollectionNamespace.CollectionName, revision.TemplateId, result, count);
        }
    }

    private sealed record ContentRevision(Guid TemplateId, BsonDocument Before, BsonDocument StarsBefore,
        BsonDocument After, BsonDocument StarsAfter);
}
