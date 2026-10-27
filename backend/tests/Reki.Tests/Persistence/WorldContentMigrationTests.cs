using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Migrations;

namespace Reki.Tests.Persistence;

[ClassDataSource<TestSessionFixture>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Migrations", Order = 0)]
public sealed class WorldContentMigrationTests(TestSessionFixture mongo)
{
    [Test]
    public async Task UpAsync_ActiveCatalog_UpdatesFourWorldsWithoutChangingIdentityAsync()
    {
        // Arrange
        await using var scope = await CreateDatabaseAsync();
        var collection = scope.Database.GetCollection<BsonDocument>("world_templates");
        var before = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        // Act
        await RefreshAsync(scope.Database);
        var after = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        // Assert
        await Assert.That(after.Count).IsEqualTo(before.Count);
        foreach (var original in before)
        {
            var saved = after.Single(item => item["_id"] == original["_id"]);
            if (original["IsArchived"].AsBoolean)
            {
                await Assert.That(saved).IsEqualTo(original);
                continue;
            }

            await Assert.That(saved["Interface"]["Description"] != original["Interface"]["Description"]).IsTrue();
            await Assert.That(saved["Interface"]["FirstContext"] != original["Interface"]["FirstContext"]).IsTrue();
            await Assert.That(saved["Rules"].AsBsonArray.Any(rule => rule["Text"] == "Без тем 18+ и травли по внешности")).IsFalse();
            await Assert.That(WithoutContent(saved)).IsEqualTo(WithoutContent(original));
        }

        var evening = after.Single(item => item["Interface"]["Title"] == "Вечерело");
        var eduard = evening["Stars"].AsBsonArray.Single(star => star["_id"] == "eduard");
        await Assert.That(eduard["About"].AsString.Contains("добычу", StringComparison.Ordinal)).IsTrue();
        await Assert.That(eduard["About"].AsString.Contains("Зависит", StringComparison.Ordinal)).IsTrue();
        var houses = after.Single(item => item["Interface"]["Title"] == "Игра домов");
        await Assert.That(houses["Stars"].AsBsonArray.Single(star => star["_id"] == "james")["About"].AsString
            .Contains("правой кисти", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task UpAsync_SelectedPersona_PreservesExistingStateOnRetryAsync()
    {
        // Arrange
        await using var scope = await CreateDatabaseAsync();
        var database = scope.Database;
        var before = await CreateInstanceAsync(database);
        const string suffix = "\nПерсона игрока: Изабелла Лебедева. Моя сохранённая биография.\nМои заметки.";
        before["Interface"]["Description"] = before["Interface"]["Description"].AsString + suffix;
        before["Stars"] = new BsonArray(before["Stars"].AsBsonArray.Where(star => star["_id"] != "isabella").Reverse());
        before["Stars"][0]["AvatarSeed"] = "my-custom-nickname";
        before["Stars"][0]["Relation"] = 4;
        before["Stars"][0]["RelationGainedInPeriod"] = 1;
        before["Version"] = 23;
        before["Player"]["Subscribers"] = 921L;
        before["Player"]["Name"] = "Мой герой";
        await SaveInstanceAsync(database, before);
        var post = new BsonDocument
        {
            ["_id"] = "saved-post",
            ["WorldId"] = before["_id"],
            ["Text"] = "Сохранённый пост",
            ["Comments"] = new BsonArray { new BsonDocument("Text", "Сохранённый ответ") },
        };
        await database.GetCollection<BsonDocument>("posts").InsertOneAsync(post);
        var profile = new BsonDocument { ["_id"] = before["Player"]["PlayerId"], ["EnergyLeft"] = 7, ["Skills"] = new BsonDocument("honest", 12) };
        await database.GetCollection<BsonDocument>("players").InsertOneAsync(profile);

        // Act
        await RefreshAsync(database);
        var first = await ReadInstanceAsync(database, before["_id"]);
        await RefreshAsync(database);
        var second = await ReadInstanceAsync(database, before["_id"]);

        // Assert
        await Assert.That(second).IsEqualTo(first);
        await Assert.That(WithoutContent(first)).IsEqualTo(WithoutContent(before));
        await Assert.That(first["Interface"]["Description"].AsString.EndsWith(suffix, StringComparison.Ordinal)).IsTrue();
        await Assert.That(first["Interface"]["Description"] != before["Interface"]["Description"]).IsTrue();
        await Assert.That(first["Stars"].AsBsonArray.Count).IsEqualTo(2);
        await Assert.That(first["Stars"][0]["About"] != before["Stars"][0]["About"]).IsTrue();
        await Assert.That(await database.GetCollection<BsonDocument>("posts").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(post);
        await Assert.That(await database.GetCollection<BsonDocument>("players").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(profile);
    }

    [Test]
    [Arguments("description")]
    [Arguments("role")]
    [Arguments("about")]
    [Arguments("voice")]
    [Arguments("rule")]
    [Arguments("locked")]
    [Arguments("ideas")]
    [Arguments("name")]
    [Arguments("unknown-npc")]
    [Arguments("duplicate-npc")]
    [Arguments("partial-update")]
    public async Task UpAsync_EditedOrUnknownBundle_SkipsEntireWorldWithoutLoggingPlayerTextAsync(string edit)
    {
        // Arrange
        await using var scope = await CreateDatabaseAsync();
        var database = scope.Database;
        var before = await CreateInstanceAsync(database);
        const string privateText = "PRIVATE_PLAYER_TEXT_NOT_FOR_LOGS";
        switch (edit)
        {
            case "description":
            {
                before["Interface"]["Description"] = privateText;
                break;
            }

            case "role":
            {
                before["Stars"][0]["Role"] = privateText;
                break;
            }

            case "about":
            {
                before["Stars"][0]["About"] = privateText;
                break;
            }

            case "voice":
            {
                before["Stars"][0]["Voice"] = privateText;
                break;
            }

            case "rule":
            {
                before["Rules"][0]["Text"] = privateText;
                break;
            }

            case "locked":
            {
                before["Rules"][0]["Locked"] = true;
                break;
            }

            case "ideas":
            {
                before["PostIdeas"].AsBsonArray.Add(privateText);
                break;
            }

            case "name":
            {
                before["Stars"][0]["Name"] = privateText;
                break;
            }

            case "unknown-npc":
            {
                before["Stars"][0]["_id"] = "unknown";
                break;
            }

            case "duplicate-npc":
            {
                before["Stars"].AsBsonArray.Add(before["Stars"][0].DeepClone());
                break;
            }

            case "partial-update":
            {
                before["Interface"]["Where"] = "дождливом городе у леса";
                break;
            }
        }

        await SaveInstanceAsync(database, before);
        var logger = new FakeLogger<RefreshWorldContent>();

        // Act
        using var session = await mongo.MongoClient.StartSessionAsync();
        await new RefreshWorldContent(logger).UpAsync(database, session, CancellationToken.None);
        var after = await ReadInstanceAsync(database, before["_id"]);
        var messages = string.Join('\n', logger.Collector.GetSnapshot().Select(record => record.Message));

        // Assert
        await Assert.That(after).IsEqualTo(before);
        await Assert.That(messages.Contains("UnknownOrEditedContent", StringComparison.Ordinal)).IsTrue();
        await Assert.That(messages.Contains(privateText, StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    [Arguments("own")]
    [Arguments("unknown-origin")]
    [Arguments("archived")]
    public async Task UpAsync_IneligibleOriginOrArchivedWorld_DoesNotTouchContentAsync(string kind)
    {
        // Arrange
        await using var scope = await CreateDatabaseAsync();
        var before = await CreateInstanceAsync(scope.Database);
        if (kind == "archived")
        {
            before["IsArchived"] = true;
        }
        else
        {
            before["TemplateId"] = kind == "own" ? BsonNull.Value : Uuid(Guid.Parse("eb318e0b-343e-454c-b426-33fb7bef2e80"));
        }

        await SaveInstanceAsync(scope.Database, before);

        // Act
        await RefreshAsync(scope.Database);

        // Assert
        await Assert.That(await ReadInstanceAsync(scope.Database, before["_id"])).IsEqualTo(before);
    }

    [Test]
    [Arguments("owner")]
    [Arguments("origin")]
    public async Task UpAsync_NonCatalogDocumentWithCatalogId_IsNotTreatedAsTemplateAsync(string kind)
    {
        // Arrange
        await using var scope = await CreateDatabaseAsync();
        var collection = scope.Database.GetCollection<BsonDocument>("world_templates");
        var before = await collection.Find(new BsonDocument("Interface.Title", "Вечерело")).SingleAsync();
        if (kind == "owner")
        {
            before["Player"]["PlayerId"] = Uuid(Guid.Parse("e270b536-a7cd-4de9-bf3b-641ba4ebda82"));
        }
        else
        {
            before["TemplateId"] = before["_id"];
        }

        await collection.ReplaceOneAsync(new BsonDocument("_id", before["_id"]), before);

        // Act
        await RefreshAsync(scope.Database);

        // Assert
        await Assert.That(await collection.Find(new BsonDocument("_id", before["_id"])).SingleAsync()).IsEqualTo(before);
    }

    [Test]
    public async Task UpAsync_AbortedTransaction_DoesNotLeavePartialContentAsync()
    {
        // Arrange
        await using var scope = await CreateDatabaseAsync();
        var before = await CreateInstanceAsync(scope.Database);
        await SaveInstanceAsync(scope.Database, before);
        using var session = await mongo.MongoClient.StartSessionAsync();
        session.StartTransaction();

        // Act
        await new RefreshWorldContent(NullLogger<RefreshWorldContent>.Instance).UpAsync(scope.Database, session, CancellationToken.None);
        await session.AbortTransactionAsync();

        // Assert
        await Assert.That(await ReadInstanceAsync(scope.Database, before["_id"])).IsEqualTo(before);
    }

    private static async Task<BsonDocument> CreateInstanceAsync(IMongoDatabase database)
    {
        var template = await database.GetCollection<BsonDocument>("world_templates").Find(new BsonDocument("Interface.Title", "Вечерело")).SingleAsync();
        var world = template.DeepClone().AsBsonDocument;
        world["TemplateId"] = template["_id"];
        world["_id"] = Uuid(Guid.NewGuid());
        world["Player"]["PlayerId"] = Uuid(Guid.Parse("748831a0-006f-44c0-9182-215f46aad2a7"));
        return world;
    }

    private static BsonBinaryData Uuid(Guid value) => new(value, GuidRepresentation.Standard);

    private static Task SaveInstanceAsync(IMongoDatabase database, BsonDocument world) =>
        database.GetCollection<BsonDocument>("worlds").InsertOneAsync(world);

    private static Task<BsonDocument> ReadInstanceAsync(IMongoDatabase database, BsonValue id) =>
        database.GetCollection<BsonDocument>("worlds").Find(new BsonDocument("_id", id)).SingleAsync();

    private static BsonDocument WithoutContent(BsonDocument source)
    {
        var result = source.DeepClone().AsBsonDocument;
        foreach (var name in new[] { "Roles", "Voices", "PostIdeas", "DemoPosts", "Rules" })
        {
            result.Remove(name);
        }

        foreach (var name in new[] { "Description", "Where", "Summary", "FirstContext", "FirstPrompt" })
        {
            result["Interface"].AsBsonDocument.Remove(name);
        }

        foreach (var star in result["Stars"].AsBsonArray)
        {
            star.AsBsonDocument.Remove("Role");
            star.AsBsonDocument.Remove("About");
            star.AsBsonDocument.Remove("Voice");
        }

        return result;
    }

    private async Task<DatabaseScope> CreateDatabaseAsync()
    {
        var scope = new DatabaseScope(mongo.MongoClient);
        using var session = await mongo.MongoClient.StartSessionAsync();
        await new SeedWorldTemplates().UpAsync(scope.Database, session, CancellationToken.None);
        await new ReleaseWorldCatalog().UpAsync(scope.Database, session, CancellationToken.None);
        await new RemoveStarAppearanceFields().UpAsync(scope.Database, session, CancellationToken.None);
        await new UnifyWorldBalance().UpAsync(scope.Database, session, CancellationToken.None);
        return scope;
    }

    private async Task RefreshAsync(IMongoDatabase database)
    {
        using var session = await mongo.MongoClient.StartSessionAsync();
        session.StartTransaction();
        await new RefreshWorldContent(NullLogger<RefreshWorldContent>.Instance).UpAsync(database, session, CancellationToken.None);
        await session.CommitTransactionAsync();
    }

    private sealed class DatabaseScope(IMongoClient client) : IAsyncDisposable
    {
        public IMongoDatabase Database { get; } = client.GetDatabase($"content_migration_{Guid.NewGuid():N}");

        public ValueTask DisposeAsync() => new(client.DropDatabaseAsync(Database.DatabaseNamespace.DatabaseName, CancellationToken.None));
    }
}
