using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Posts;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class UnifyPostContent : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 2, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var posts = db.GetCollection<BsonDocument>(MongoCollections.Posts);
        using var indexes = await posts.Indexes.ListAsync(cancellationToken);
        var existingIndexes = await indexes.ToListAsync(cancellationToken);
        var key = new BsonDocument { ["WorldId"] = 1, ["CreatedAtUtc"] = -1 };
        var existing = existingIndexes.SingleOrDefault(index => index["name"] == "ix_posts_world_created");
        if (existing is not null && existing["key"] != key)
        {
            await posts.Indexes.DropOneAsync("ix_posts_world_created", cancellationToken);
        }

        if (existingIndexes.Any(index => index["name"] == "ix_posts_player_created"))
        {
            await posts.Indexes.DropOneAsync("ix_posts_player_created", cancellationToken);
        }

        await posts.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(key, new CreateIndexOptions { Name = "ix_posts_world_created" }),
            cancellationToken: cancellationToken);

        var worlds = db.GetCollection<BsonDocument>(MongoCollections.Worlds);
        using var cursor = await posts.FindAsync(session, FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            await MigratePostBatchAsync(posts, worlds, session, cursor.Current, cancellationToken);
        }

        await MigratePublicationResultsAsync(db, session, worlds, cancellationToken);
        await MigratePlayersAsync(db, session, cancellationToken);
        await worlds.UpdateManyAsync(session, new BsonDocument("Version", BsonNull.Value), Builders<BsonDocument>.Update.Set("Version", 0), cancellationToken: cancellationToken);
    }

    private static Task<BsonDocument> FindWorldAsync(IMongoCollection<BsonDocument> worlds, IClientSessionHandle session, BsonDocument post, CancellationToken cancellationToken) =>
        worlds.Find(session, new BsonDocument("_id", post["WorldId"])).SingleAsync(cancellationToken);

    private static BsonDocument MigratePost(BsonDocument source, BsonDocument world)
    {
        var post = source.DeepClone().AsBsonDocument;
        var comments = ReadArray(post, "Comments").Select(value => MigrateComment(value.AsBsonDocument, world)).ToArray();
        foreach (var reply in ReadArray(post, "ThreadReplies").Select(entry => entry.AsBsonDocument))
        {
            var comment = comments.Single(item => item["_id"] == reply["CommentId"]);
            comment["ParentCommentId"] = reply["TargetCommentId"];
            comment["RequestId"] = reply["ReplyId"];
            var reaction = reply["Reaction"].AsString;
            if (reaction.Length > 0)
            {
                var target = comments.Single(item => item["_id"] == reply["TargetCommentId"]);
                comment["Reactions"].AsBsonArray.Add(new BsonDocument
                {
                    ["AuthorId"] = target["AuthorId"],
                    ["Emoji"] = reaction,
                });
            }
        }

        post["AuthorId"] = Author(AuthorKind.Player, post["PlayerId"].AsString);
        post["Comments"] = new BsonArray(comments);
        post["Version"] = 0;
        post.Remove("PlayerId");
        post.Remove("ThreadReplies");
        return post;
    }

    private static BsonDocument MigrateComment(BsonDocument source, BsonDocument world) => new()
    {
        ["_id"] = source["_id"],
        ["ParentCommentId"] = BsonNull.Value,
        ["RequestId"] = BsonNull.Value,
        ["AuthorId"] = ResolveLegacyAuthor(source, world),
        ["Text"] = source["Text"],
        ["Hidden"] = source.GetValue("Hidden", false),
        ["Reactions"] = new BsonArray(ReadArray(source, "Reactions").Select(value =>
        {
            var reaction = value.AsBsonDocument;
            return new BsonDocument
            {
                ["AuthorId"] = ResolveLegacyAuthor(reaction, world),
                ["Emoji"] = reaction["Emoji"],
            };
        })),
    };

    private static BsonString ResolveLegacyAuthor(BsonDocument source, BsonDocument world)
    {
        if (source.GetValue("Me", false).AsBoolean)
        {
            return Author(AuthorKind.Player, world["Player"]["PlayerId"].AsString);
        }

        var stars = world["Stars"].AsBsonArray.Select(value => value.AsBsonDocument).ToArray();
        var stable = stars.Where(star =>
            source.GetValue("Avatar", BsonNull.Value) == star["_id"] ||
            source.GetValue("Handle", BsonNull.Value) == $"@{star["_id"].AsString}").ToArray();
        var candidates = stable.Length > 0 ? stable : stars.Where(star => source.GetValue("Author", BsonNull.Value) == star["Name"]).ToArray();
        return candidates is [var resolved]
            ? Author(AuthorKind.Star, resolved["_id"].AsString)
            : throw new InvalidOperationException("Legacy comment author cannot be resolved. Resolve the source data before rerunning migration 2.2.0.");
    }

    private static async Task MigratePublicationResultsAsync(IMongoDatabase db, IClientSessionHandle session, IMongoCollection<BsonDocument> worlds, CancellationToken cancellationToken)
    {
        var publications = db.GetCollection<BsonDocument>(MongoCollections.Publications);
        using var cursor = await publications.FindAsync(session, new BsonDocument("Result.Post", new BsonDocument("$type", "object")), cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            await MigratePublicationBatchAsync(publications, worlds, session, cursor.Current, cancellationToken);
        }
    }

    private static async Task MigratePlayersAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        using var cursor = await players.FindAsync(session, FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            await MigratePlayerBatchAsync(players, session, cursor.Current, cancellationToken);
        }
    }

    private static async Task MigratePostBatchAsync(IMongoCollection<BsonDocument> posts, IMongoCollection<BsonDocument> worlds, IClientSessionHandle session, IEnumerable<BsonDocument> batch, CancellationToken cancellationToken)
    {
        foreach (var post in batch)
        {
            var world = await FindWorldAsync(worlds, session, post, cancellationToken);
            var migrated = MigratePost(post, world);
            await posts.ReplaceOneAsync(session, new BsonDocument("_id", post["_id"]), migrated, cancellationToken: cancellationToken);
        }
    }

    private static async Task MigratePublicationBatchAsync(IMongoCollection<BsonDocument> publications, IMongoCollection<BsonDocument> worlds, IClientSessionHandle session, IEnumerable<BsonDocument> batch, CancellationToken cancellationToken)
    {
        foreach (var publication in batch)
        {
            var post = publication["Result"].AsBsonDocument["Post"].AsBsonDocument;
            var world = await FindWorldAsync(worlds, session, post, cancellationToken);
            var migrated = MigratePost(post, world);
            await publications.UpdateOneAsync(session, new BsonDocument("_id", publication["_id"]), Builders<BsonDocument>.Update.Set("Result.Post", migrated), cancellationToken: cancellationToken);
        }
    }

    private static async Task MigratePlayerBatchAsync(IMongoCollection<BsonDocument> players, IClientSessionHandle session, IEnumerable<BsonDocument> batch, CancellationToken cancellationToken)
    {
        foreach (var player in batch)
        {
            var update = Builders<BsonDocument>.Update.Set("EnergyUsed", player.GetValue("PostsUsed", 0))
                .Set("Version", ReadVersion(player)).Unset("PostsUsed");
            await players.UpdateOneAsync(session, new BsonDocument("_id", player["_id"]), update, cancellationToken: cancellationToken);
        }
    }

    private static BsonValue ReadVersion(BsonDocument document) =>
        document.TryGetValue("Version", out var value) && !value.IsBsonNull ? value : new BsonInt32(0);

    private static BsonArray ReadArray(BsonDocument document, string field) =>
        document.TryGetValue(field, out var value) && !value.IsBsonNull ? value.AsBsonArray : [];

    private static BsonString Author(AuthorKind kind, string id) => new(AuthorId.From($"{kind}_{id}").Value);
}
