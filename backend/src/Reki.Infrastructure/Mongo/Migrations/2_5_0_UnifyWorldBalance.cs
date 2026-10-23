using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs.GenerateStarFeedPost;
using Reki.Infrastructure.Mongo.Repositories;
using TickerQ.Utilities;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class UnifyWorldBalance : RekiMigrationBase
{
    private static readonly IReadOnlyDictionary<string, Guid> TemplateIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["stage"] = Guid.Parse("edfccf53-65c6-4eaf-8321-2391bc1758bd"),
        ["academy"] = Guid.Parse("b018145e-c887-4d00-9f18-5c877d0af8be"),
        ["it"] = Guid.Parse("5720946a-ea7c-4a10-9c50-51ce3deefc53"),
        ["stream"] = Guid.Parse("0cb3f809-6dfd-4ef1-af52-daa12a5b8804"),
        ["own"] = Guid.Parse("14049891-fe2b-4d98-b38f-0f37e6544052"),
        ["evening"] = Guid.Parse("b3a1a522-0263-4796-a197-57a8731fc291"),
        ["sorcerers"] = Guid.Parse("aec9494b-4118-4525-8458-e85d759c8dd9"),
        ["houses"] = Guid.Parse("8c02a7fb-3b55-4eb0-b957-1b4869ed9834"),
    };

    public override DatabaseVersion Version => new(2, 5, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        // The migrator owns the transaction. All mappings are confined to this attempt.
        var templates = await ReadWorldsAsync(db, session, MongoCollections.WorldTemplates, cancellationToken);
        var worlds = await ReadWorldsAsync(db, session, MongoCollections.Worlds, cancellationToken);
        var ids = new Dictionary<BsonValue, BsonBinaryData>();
        var profiles = new Dictionary<BsonValue, BsonDocument>();
        foreach (var document in templates.Concat(worlds))
        {
            var previous = document["_id"];
            if (!ids.TryAdd(previous, ConvertId(previous)))
            {
                throw new InvalidOperationException("World identifiers are not globally unique.");
            }

            profiles[previous] = Snapshot(document);
        }

        if (ids.Values.Distinct().Count() != ids.Count)
        {
            throw new InvalidOperationException("World identifier conversion has a collision.");
        }

        await RewriteWorldsAsync(db, session, MongoCollections.WorldTemplates, templates, ids, cancellationToken);
        await RestoreTemplatesAsync(db, session, cancellationToken);
        await RewriteWorldsAsync(db, session, MongoCollections.Worlds, worlds, ids, cancellationToken);
        foreach (var collectionName in new[] { MongoCollections.Posts, MongoCollections.Publications, MongoCollections.Events })
        {
            await RewriteReferencesAsync(db, session, collectionName, ids, profiles, cancellationToken);
        }

        await RewriteTickersAsync(db, session, ids, cancellationToken);
    }

    private static Task<List<BsonDocument>> ReadWorldsAsync(IMongoDatabase db, IClientSessionHandle session, string name, CancellationToken cancellationToken) =>
        db.GetCollection<BsonDocument>(name).Find(session, FilterDefinition<BsonDocument>.Empty).ToListAsync(cancellationToken);

    private static BsonBinaryData ConvertId(BsonValue value)
    {
        if (value is BsonBinaryData { SubType: BsonBinarySubType.UuidStandard } binary)
        {
            return binary;
        }

        if (!value.IsString)
        {
            throw new InvalidOperationException("Unsupported historical world identifier.");
        }

        var text = value.AsString;
        if (TemplateIds.TryGetValue(text, out var template))
        {
            return new BsonBinaryData(template, GuidRepresentation.Standard);
        }

        if (!Guid.TryParse(text, out var guid))
        {
            // Early instances used player-id + setting. Stable namespace preserves retries without a DB registry.
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes("reki:world:2.5.0:" + text));
            guid = new Guid(hash.AsSpan(0, 16));
        }

        return new BsonBinaryData(guid, GuidRepresentation.Standard);
    }

    private static string? LegacyKey(BsonDocument document) =>
        document.TryGetValue("BalanceKey", out var key) && key.IsString ? key.AsString : null;

    private static WorldDifficulty Difficulty(BsonDocument document) => LegacyKey(document) switch
    {
        "stage" or "evening" => WorldDifficulty.Easy,
        "academy" or "stream" or "sorcerers" or "houses" or "own" => WorldDifficulty.Normal,
        "it" => WorldDifficulty.Hard,
        null when document.GetValue("Difficulty", BsonNull.Value).IsString => Enum.Parse<WorldDifficulty>(document["Difficulty"].AsString, ignoreCase: true),
        _ => throw new InvalidOperationException("The mechanical difficulty cannot be resolved."),
    };

    private static BsonDocument Snapshot(BsonDocument document)
    {
        var difficulty = Difficulty(document);
        var own = LegacyKey(document) == "own";
        var coefficients = difficulty switch
        {
            WorldDifficulty.Easy => new BsonDocument { { "GrowthCoefficient", 1.0 }, { "FlopChance", 0.10 }, { "ViralMultiplier", 1.0 }, { "HateFrom", 5000L } },
            WorldDifficulty.Normal => new BsonDocument { { "GrowthCoefficient", own ? 1.0 : 1.1 }, { "FlopChance", 0.15 }, { "ViralMultiplier", own ? 1.0 : 1.2 }, { "HateFrom", own ? 5000L : 3000L } },
            WorldDifficulty.Hard => new BsonDocument { { "GrowthCoefficient", 1.25 }, { "FlopChance", 0.20 }, { "ViralMultiplier", 1.4 }, { "HateFrom", 1000L } },
            _ => throw new InvalidOperationException("Unknown mechanical difficulty."),
        };
        return new BsonDocument { { "Difficulty", difficulty.ToString() }, { "Coefficients", coefficients } };
    }

    private static async Task RewriteWorldsAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        string name,
        IEnumerable<BsonDocument> documents,
        IReadOnlyDictionary<BsonValue, BsonBinaryData> ids,
        CancellationToken cancellationToken)
    {
        var collection = db.GetCollection<BsonDocument>(name);
        foreach (var document in documents)
        {
            var previousId = document["_id"];
            var isTemplate = name == MongoCollections.WorldTemplates;
            var key = LegacyKey(document);
            document["Difficulty"] = Difficulty(document).ToString();
            document["_id"] = ids[previousId];
            if (isTemplate)
            {
                document["TemplateId"] = BsonNull.Value;
                document["Player"]["PlayerId"] = new BsonBinaryData(Guid.Empty, GuidRepresentation.Standard);
                document["IsArchived"] = IsHistoricalTemplate(document["_id"]) || document.GetValue("IsArchived", false).AsBoolean;
            }
            else if (key is not null)
            {
                document["TemplateId"] = key == "own" ? BsonNull.Value : new BsonBinaryData(TemplateIds[key], GuidRepresentation.Standard);
            }
            else if (document.TryGetValue("TemplateId", out var templateId) && !templateId.IsBsonNull)
            {
                document["TemplateId"] = ConvertId(templateId);
            }

            if (!document.Contains("TemplateId"))
            {
                document["TemplateId"] = BsonNull.Value;
            }

            document["IsArchived"] = document.GetValue("IsArchived", false);
            if (!isTemplate && !document["TemplateId"].IsBsonNull &&
                !await db.GetCollection<BsonDocument>(MongoCollections.WorldTemplates)
                    .Find(session, new BsonDocument("_id", document["TemplateId"])).AnyAsync(cancellationToken))
            {
                throw new InvalidOperationException("An instance references a missing template.");
            }

            document.Remove("BalanceKey");
            document["Interface"].AsBsonDocument.Remove("Difficulty");
            if (previousId != document["_id"])
            {
                await collection.DeleteOneAsync(session, new BsonDocument("_id", previousId), cancellationToken: cancellationToken);
                await collection.InsertOneAsync(session, document, cancellationToken: cancellationToken);
            }
            else
            {
                await collection.ReplaceOneAsync(session, new BsonDocument("_id", previousId), document, cancellationToken: cancellationToken);
            }
        }
    }

    private static bool IsHistoricalTemplate(BsonValue id) =>
        TemplateIds.Where(pair => pair.Key is "stage" or "academy" or "stream" or "own")
            .Any(pair => id == new BsonBinaryData(pair.Value, GuidRepresentation.Standard));

    private static async Task RestoreTemplatesAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var templates = db.GetCollection<BsonDocument>(MongoCollections.WorldTemplates);
        foreach (var template in SeedWorldTemplates.CreateTemplates())
        {
            var document = template.ToBsonDocument();
            document["IsArchived"] = IsHistoricalTemplate(document["_id"]);
            await templates.UpdateOneAsync(
                session,
                new BsonDocument("_id", document["_id"]),
                new BsonDocument("$setOnInsert", document),
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }
    }

    private static async Task RewriteReferencesAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        string name,
        IReadOnlyDictionary<BsonValue, BsonBinaryData> ids,
        IReadOnlyDictionary<BsonValue, BsonDocument> profiles,
        CancellationToken cancellationToken)
    {
        var collection = db.GetCollection<BsonDocument>(name);
        using var cursor = await collection.FindAsync(session, FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        await cursor.ForEachAsync(
            async document =>
            {
                if (name == MongoCollections.Publications && document.TryGetValue("WorldId", out var worldId) &&
                    (!document.Contains("BalanceSnapshot") || document["BalanceSnapshot"].IsBsonNull))
                {
                    document["BalanceSnapshot"] = ResolveProfile(worldId, ids, profiles).DeepClone();
                }

                RewriteReferenceFields(document, ids);
                if (name == MongoCollections.Publications && document.Contains("Text"))
                {
                    var world = document["WorldId"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard);
                    var text = document["Text"].AsString;
                    var wantId = document.GetValue("WantId", BsonNull.Value);
                    var payload = $"{world:D}:{text.Length}:{text}:{document["OwnWords"].AsBoolean}:{(wantId.IsBsonNull ? null : wantId.AsString)}";
                    document["RequestHash"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
                }

                await collection.ReplaceOneAsync(session, new BsonDocument("_id", document["_id"]), document, cancellationToken: cancellationToken);
            },
            cancellationToken);
    }

    private static BsonDocument ResolveProfile(BsonValue worldId, IReadOnlyDictionary<BsonValue, BsonBinaryData> ids, IReadOnlyDictionary<BsonValue, BsonDocument> profiles)
    {
        if (profiles.TryGetValue(worldId, out var profile))
        {
            return profile;
        }

        var source = ids.FirstOrDefault(pair => pair.Value == ConvertId(worldId));
        return source.Key is not null && profiles.TryGetValue(source.Key, out profile)
            ? profile : throw new InvalidOperationException("A publication references a missing world.");
    }

    private static void RewriteReferenceFields(BsonValue value, IReadOnlyDictionary<BsonValue, BsonBinaryData> ids)
    {
        if (value is BsonDocument document)
        {
            foreach (var element in document.Elements.ToArray())
            {
                if (element.Name is "WorldId" or "TemplateId" && !element.Value.IsBsonNull)
                {
                    document[element.Name] = ResolveReference(element.Value, ids);
                }
                else
                {
                    RewriteReferenceFields(element.Value, ids);
                }
            }
        }
        else if (value is BsonArray array)
        {
            foreach (var item in array)
            {
                RewriteReferenceFields(item, ids);
            }
        }
    }

    private static BsonBinaryData ResolveReference(BsonValue value, IReadOnlyDictionary<BsonValue, BsonBinaryData> ids)
    {
        if (ids.TryGetValue(value, out var id))
        {
            return id;
        }

        var converted = ConvertId(value);
        return ids.Values.Contains(converted) ? converted : throw new InvalidOperationException("A document references a missing world.");
    }

    private static async Task RewriteTickersAsync(IMongoDatabase db, IClientSessionHandle session, IReadOnlyDictionary<BsonValue, BsonBinaryData> ids, CancellationToken cancellationToken)
    {
        var collection = db.GetCollection<BsonDocument>("ticker_TimeTickers");
        using var cursor = await collection.FindAsync(session, new BsonDocument("Function", nameof(GenerateStarFeedPostJob)), cancellationToken: cancellationToken);
        await cursor.ForEachAsync(
            async ticker =>
            {
                var payload = JsonNode.Parse(TickerHelper.ReadTickerRequestAsString(ticker["Request"].AsBsonBinaryData.Bytes))?.AsObject()
                    ?? throw new InvalidOperationException("Missing scheduled world request.");
                var key = payload.ContainsKey("worldId") ? "worldId" : "WorldId";
                var previous = new BsonString(payload[key]?.GetValue<string>()
                    ?? throw new InvalidOperationException("Missing scheduled world identifier."));
                payload.Remove("WorldId");
                payload["worldId"] = ResolveReference(previous, ids).ToGuid(GuidRepresentation.Standard).ToString("D");
                ticker["Request"] = new BsonBinaryData(TickerHelper.CreateTickerRequest(Encoding.UTF8.GetBytes(payload.ToJsonString())));
                await collection.ReplaceOneAsync(session, new BsonDocument("_id", ticker["_id"]), ticker, cancellationToken: cancellationToken);
            },
            cancellationToken);
    }
}
