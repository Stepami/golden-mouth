using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Persistence.Migrations;

namespace Reki.Tests.Persistence;

[ClassDataSource<TestSessionFixture>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Migrations", Order = 0)]
public sealed class AuthorThreadMigrationTests(TestSessionFixture mongo)
{
    [Test]
    public async Task Up_LegacyThread_PreservesAuthorsParentsRequestsReactionsAsync()
    {
        // Arrange
        var database = mongo.MongoClient.GetDatabase($"author_migration_{Guid.NewGuid():N}");
        try
        {
            var post = LegacyPost();
            var root = LegacyComment("root", false);
            root["Reactions"] = new BsonArray { new BsonDocument { ["Me"] = true, ["Emoji"] = "❤️" } };
            var reply = LegacyComment("reply", true);
            reply["EnergySpent"] = 0;
            reply["SubscriberBonus"] = 9;
            reply["OriginCommentId"] = "root";
            var followup = LegacyComment("followup", false);
            followup["ParentCommentId"] = "reply";
            post["Comments"] = new BsonArray { root, reply, followup };
            post["ThreadReplies"] = new BsonArray
            {
                new BsonDocument { ["CommentId"] = "reply", ["TargetCommentId"] = "root", ["ReplyId"] = "saved-key", ["Reaction"] = "👏" },
            };
            await SeedAsync(database, post);

            // Act
            await MigrateAsync(database);
            var saved = await ReadAsync(database, "posts", "post");
            var comments = saved["Comments"].AsBsonArray;
            await MigrateAsync(database);
            await Assert.That(await ReadAsync(database, "posts", "post")).IsEqualTo(saved);

            // Assert
            await Assert.That(saved["AuthorId"]).IsEqualTo(Author(0, "owner"));
            await Assert.That(saved["WorldId"]).IsEqualTo(new BsonString("world"));
            await Assert.That(saved.Contains("PlayerId") || saved.Contains("ThreadReplies")).IsFalse();
            await Assert.That(comments[0]["AuthorId"]).IsEqualTo(Author(1, "mira"));
            await Assert.That(comments[0]["ParentCommentId"].IsBsonNull).IsTrue();
            await Assert.That(comments[0]["RequestId"].IsBsonNull).IsTrue();
            await Assert.That(comments[0]["Reactions"]).IsEqualTo(new BsonArray { new BsonDocument { ["AuthorId"] = Author(0, "owner"), ["Emoji"] = "❤️" } });
            await Assert.That(comments[1]["AuthorId"]).IsEqualTo(Author(0, "owner"));
            await Assert.That(comments[1]["ParentCommentId"]).IsEqualTo(new BsonString("root"));
            await Assert.That(comments[1]["RequestId"]).IsEqualTo(new BsonString("saved-key"));
            await Assert.That(comments[1]["Reactions"]).IsEqualTo(new BsonArray { new BsonDocument { ["AuthorId"] = Author(1, "mira"), ["Emoji"] = "👏" } });
            await Assert.That(comments[2]["ParentCommentId"]).IsEqualTo(new BsonString("reply"));
            foreach (var comment in comments.Select(value => value.AsBsonDocument))
            {
                await Assert.That(comment.Names.Order(StringComparer.Ordinal)).IsEquivalentTo(new[] { "_id", "AuthorId", "Hidden", "ParentCommentId", "Reactions", "RequestId", "Text" });
            }

            await Assert.That((await ReadAsync(database, "players", "owner"))["EnergyUsed"]).IsEqualTo(new BsonInt32(4));
            await Assert.That((await ReadAsync(database, "worlds", "world"))["Player"]["Subscribers"]).IsEqualTo(new BsonInt32(1000));
        }
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    [Arguments("StarId")]
    [Arguments("Avatar")]
    [Arguments("Handle")]
    [Arguments("Author")]
    public async Task Up_LegacyStarIdentity_ResolvesWorldStarAsync(string identityField)
    {
        // Arrange
        var database = mongo.MongoClient.GetDatabase($"author_identity_{Guid.NewGuid():N}");
        try
        {
            var post = LegacyPost();
            var identity = identityField switch { "Handle" => "@mira", "Author" => "Мира", _ => "mira" };
            var comment = new BsonDocument { ["Id"] = "root", ["Text"] = "Старый комментарий", [identityField] = identity };
            post["Comments"] = new BsonArray { comment };
            await SeedAsync(database, post);

            // Act
            await MigrateAsync(database);

            // Assert
            var saved = await ReadAsync(database, "posts", "post");
            await Assert.That(saved["Comments"][0]["_id"]).IsEqualTo(new BsonString("root"));
            await Assert.That(saved["Comments"][0]["AuthorId"]).IsEqualTo(Author(1, "mira"));
            await Assert.That(saved["Comments"][0]["Reactions"]).IsEqualTo(new BsonArray());
        }
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, false)]
    [Arguments(false, true)]
    public async Task Up_HistoricalUsage_PreservesCountersVersionsPublicationResultAsync(bool existingVersions, bool nullVersions)
    {
        // Arrange
        var database = mongo.MongoClient.GetDatabase($"author_history_{Guid.NewGuid():N}");
        try
        {
            var post = LegacyPost();
            if (existingVersions)
            {
                post["Version"] = 7;
                post["AuthorId"] = Author(1, "mira");
                post.Remove("PlayerId");
            }

            if (nullVersions)
            {
                post["Version"] = BsonNull.Value;
                post["Comments"] = BsonNull.Value;
                post["ThreadReplies"] = BsonNull.Value;
            }

            await SeedAsync(database, post);
            if (nullVersions)
            {
                await database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", "owner"), Builders<BsonDocument>.Update.Set("Version", BsonNull.Value));
                await database.GetCollection<BsonDocument>("worlds").UpdateOneAsync(new BsonDocument("_id", "world"), Builders<BsonDocument>.Update.Set("Version", BsonNull.Value));
            }

            await database.GetCollection<BsonDocument>("publications").InsertOneAsync(new BsonDocument
            {
                ["_id"] = "publication",
                ["Result"] = new BsonDocument { ["Post"] = post.DeepClone(), ["Subscribers"] = 1234 },
            });
            if (existingVersions)
            {
                await database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", "owner"), Builders<BsonDocument>.Update.Set("Version", 8).Set("EnergyUsed", 4));
                await database.GetCollection<BsonDocument>("worlds").UpdateOneAsync(new BsonDocument("_id", "world"), Builders<BsonDocument>.Update.Set("Version", 9));
            }

            // Act
            await MigrateAsync(database);
            var saved = await ReadAsync(database, "posts", "post");
            var player = await ReadAsync(database, "players", "owner");
            var world = await ReadAsync(database, "worlds", "world");
            var publication = await ReadAsync(database, "publications", "publication");
            await MigrateAsync(database);

            // Assert
            await Assert.That(saved["Comments"]).IsEqualTo(new BsonArray());
            await Assert.That(saved["Version"]).IsEqualTo(new BsonInt32(existingVersions ? 7 : 0));
            await Assert.That(saved["AuthorId"]).IsEqualTo(Author(existingVersions ? 1 : 0, existingVersions ? "mira" : "owner"));
            await Assert.That(player["EnergyUsed"]).IsEqualTo(new BsonInt32(4));
            await Assert.That(player.Contains("PostsUsed")).IsFalse();
            await Assert.That(player["Version"]).IsEqualTo(new BsonInt32(existingVersions ? 8 : 0));
            await Assert.That(world["Version"]).IsEqualTo(new BsonInt32(existingVersions ? 9 : 0));
            await Assert.That(world["Player"]["Subscribers"]).IsEqualTo(new BsonInt32(1000));
            await Assert.That(publication["Result"]["Post"]).IsEqualTo(saved);
            await Assert.That(publication["Result"]["Subscribers"]).IsEqualTo(new BsonInt32(1234));
            await Assert.That(await ReadAsync(database, "posts", "post")).IsEqualTo(saved);
            await Assert.That(await ReadAsync(database, "players", "owner")).IsEqualTo(player);
            await Assert.That(await ReadAsync(database, "worlds", "world")).IsEqualTo(world);
            await Assert.That(await ReadAsync(database, "publications", "publication")).IsEqualTo(publication);
        }
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    [Arguments("unknown-author")]
    [Arguments("ambiguous-author")]
    [Arguments("missing-world")]
    [Arguments("foreign-owner")]
    [Arguments("duplicate-comment")]
    [Arguments("duplicate-request")]
    [Arguments("dangling-parent")]
    [Arguments("cycle")]
    [Arguments("usage-conflict")]
    public async Task Up_InvalidLegacyContent_RejectsWithoutPartialDataChangesAsync(string scenario)
    {
        // Arrange
        var database = mongo.MongoClient.GetDatabase($"author_invalid_{Guid.NewGuid():N}");
        try
        {
            var post = LegacyPost();
            post["Comments"] = new BsonArray { LegacyComment("root", false) };
            await SeedAsync(database, post);
            await MakeInvalidAsync(database, post, scenario);
            await database.GetCollection<BsonDocument>("posts").ReplaceOneAsync(new BsonDocument("_id", "post"), post);
            var player = await ReadAsync(database, "players", "owner");
            var world = await ReadAsync(database, "worlds", "world");

            // Act
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => MigrateAsync(database));

            // Assert
            await Assert.That((failure ?? throw new InvalidOperationException("Expected migration failure.")).Message).Contains("Resolve the source data");
            await Assert.That(await ReadAsync(database, "posts", "post")).IsEqualTo(post);
            await Assert.That(await ReadAsync(database, "players", "owner")).IsEqualTo(player);
            await Assert.That(await ReadAsync(database, "worlds", "world")).IsEqualTo(world);
        }
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    public async Task Up_TypedWorldStars_ResolvesStoredIdentifierAsync()
    {
        // Arrange
        var database = mongo.MongoClient.GetDatabase($"author_typed_world_{Guid.NewGuid():N}");
        try
        {
            var post = LegacyPost();
            post["Comments"] = new BsonArray { LegacyComment("root", false) };
            await SeedAsync(database, post);
            var template = Common.TestGameData.CreateWorld();
            var world = template with { Id = WorldId.From("world"), Player = template.Player with { PlayerId = PlayerId.From("owner") } };
            var serialized = world.ToBsonDocument();
            await Assert.That(serialized["Stars"][0].AsBsonDocument.Contains("_id")).IsTrue();
            await Assert.That(serialized["Stars"][0]["_id"]).IsEqualTo(new BsonString("mira"));
            await database.GetCollection<BsonDocument>("worlds").ReplaceOneAsync(new BsonDocument("_id", "world"), serialized);

            // Act
            await MigrateAsync(database);

            // Assert
            var saved = await ReadAsync(database, "posts", "post");
            await Assert.That(saved["AuthorId"]).IsEqualTo(Author(0, "owner"));
            await Assert.That(saved["Comments"][0]["AuthorId"]).IsEqualTo(Author(1, "mira"));
            await Assert.That(saved["Comments"][0]["_id"]).IsEqualTo(new BsonString("root"));
            await Assert.That(saved["Comments"][0].AsBsonDocument.Contains("Avatar")).IsFalse();
        }
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    private static BsonDocument Author(int kind, string id) => new() { ["Kind"] = kind, ["Id"] = id };

    private static BsonDocument LegacyPost() => new() { ["_id"] = "post", ["WorldId"] = "world", ["PlayerId"] = "owner", ["Text"] = "Пост" };

    private static BsonDocument LegacyComment(string id, bool me) => new()
    {
        ["_id"] = id,
        ["Text"] = "Комментарий",
        ["Me"] = me,
        ["Author"] = me ? "Канал" : "Мира",
        ["Avatar"] = me ? "player-avatar" : "mira",
        ["Star"] = !me,
    };

    private static async Task SeedAsync(IMongoDatabase database, BsonDocument post)
    {
        await database.GetCollection<BsonDocument>("worlds").InsertOneAsync(new BsonDocument
        {
            ["_id"] = "world",
            ["Player"] = new BsonDocument { ["PlayerId"] = "owner", ["Subscribers"] = 1000 },
            ["Stars"] = new BsonArray { new BsonDocument { ["Id"] = "mira", ["Name"] = "Мира" } },
        });
        await database.GetCollection<BsonDocument>("players").InsertOneAsync(new BsonDocument { ["_id"] = "owner", ["PostsUsed"] = 4 });
        await database.GetCollection<BsonDocument>("posts").InsertOneAsync(post);
    }

    private static Task<BsonDocument> ReadAsync(IMongoDatabase database, string collection, string id) =>
        database.GetCollection<BsonDocument>(collection).Find(new BsonDocument("_id", id)).SingleAsync();

    private static async Task MakeInvalidAsync(IMongoDatabase database, BsonDocument post, string scenario)
    {
        var root = post["Comments"][0].AsBsonDocument;
        switch (scenario)
        {
            case "unknown-author":
            {
                root["Avatar"] = "unknown";
                root["Author"] = "Неизвестно";
                break;
            }

            case "ambiguous-author":
            {
                root.Remove("Avatar");
                await database.GetCollection<BsonDocument>("worlds").UpdateOneAsync(new BsonDocument("_id", "world"), Builders<BsonDocument>.Update.Push("Stars", new BsonDocument { ["Id"] = "other", ["Name"] = "Мира" }));
                break;
            }

            case "missing-world":
            {
                post["WorldId"] = "missing";
                break;
            }

            case "foreign-owner":
            {
                post["PlayerId"] = "other-player";
                break;
            }

            case "duplicate-comment":
            {
                post["Comments"].AsBsonArray.Add(root.DeepClone());
                break;
            }

            case "duplicate-request":
            {
                root["RequestId"] = "duplicate";
                var reply = LegacyComment("reply", true);
                reply["RequestId"] = "duplicate";
                post["Comments"].AsBsonArray.Add(reply);
                break;
            }

            case "dangling-parent":
            {
                root["ParentCommentId"] = "missing";
                break;
            }

            case "cycle":
            {
                root["ParentCommentId"] = "reply";
                var reply = LegacyComment("reply", true);
                reply["ParentCommentId"] = "root";
                post["Comments"].AsBsonArray.Add(reply);
                break;
            }

            case "usage-conflict":
            {
                await database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", "owner"), Builders<BsonDocument>.Update.Set("EnergyUsed", 5));
                break;
            }

            default:
            {
                throw new ArgumentOutOfRangeException(nameof(scenario));
            }
        }
    }

    private async Task MigrateAsync(IMongoDatabase database)
    {
        using var session = await mongo.MongoClient.StartSessionAsync();
        session.StartTransaction();
        try
        {
            await new UnifyPostContent().UpAsync(database, session, CancellationToken.None);
            await session.CommitTransactionAsync();
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }
}
