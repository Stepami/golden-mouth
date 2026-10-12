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

    private static async Task<BsonDocument> FindWorldAsync(IMongoCollection<BsonDocument> worlds, IClientSessionHandle session, BsonDocument post, CancellationToken cancellationToken)
    {
        var worldId = post.GetValue("WorldId", BsonNull.Value);
        if (!worldId.IsString || string.IsNullOrWhiteSpace(worldId.AsString))
        {
            throw InvalidLegacyContent();
        }

        return await worlds.Find(session, new BsonDocument("_id", worldId)).SingleOrDefaultAsync(cancellationToken)
            ?? throw InvalidLegacyContent();
    }

    private static BsonDocument MigratePost(BsonDocument source, BsonDocument world)
    {
        var post = source.DeepClone().AsBsonDocument;
        var owner = world["Player"].AsBsonDocument["PlayerId"].AsString;
        var comments = ReadArray(post, "Comments").Select(value => MigrateComment(value.AsBsonDocument, world)).ToArray();
        if (comments.Select(comment => comment["_id"]).Distinct().Count() != comments.Length)
        {
            throw InvalidLegacyContent();
        }

        foreach (var reply in ReadArray(post, "ThreadReplies").Select(entry => entry.AsBsonDocument))
        {
            var comment = comments.SingleOrDefault(item => item["_id"] == reply["CommentId"]) ?? throw InvalidLegacyContent();
            var target = comments.SingleOrDefault(item => item["_id"] == reply["TargetCommentId"]) ?? throw InvalidLegacyContent();
            if (comment["AuthorId"] != Author(AuthorKind.Player, owner) || ReadAuthor(target["AuthorId"]).Kind != AuthorKind.Star)
            {
                throw InvalidLegacyContent();
            }

            comment["ParentCommentId"] = reply["TargetCommentId"];
            var request = reply.GetValue("ReplyId", BsonNull.Value);
            if (!request.IsString || string.IsNullOrWhiteSpace(request.AsString))
            {
                throw InvalidLegacyContent();
            }

            comment["RequestId"] = request;
            var reaction = reply.GetValue("Reaction", BsonNull.Value);
            if (!reaction.IsBsonNull && reaction.AsString.Length > 0)
            {
                comment["Reactions"].AsBsonArray.Add(new BsonDocument
                {
                    ["AuthorId"] = target["AuthorId"].DeepClone(),
                    ["Emoji"] = reaction,
                });
            }
        }

        ValidateBranches(comments);
        if (post.TryGetValue("AuthorId", out var author))
        {
            post["AuthorId"] = ValidateAuthor(author, world);
        }
        else
        {
            var playerId = post.GetValue("PlayerId", BsonNull.Value);
            if (!playerId.IsString || playerId.AsString != owner)
            {
                throw InvalidLegacyContent();
            }

            post["AuthorId"] = Author(AuthorKind.Player, owner);
        }

        post["Comments"] = new BsonArray(comments);
        post["Version"] = ReadVersion(post);
        post.Remove("PlayerId");
        post.Remove("ThreadReplies");
        return post;
    }

    private static BsonDocument MigrateComment(BsonDocument source, BsonDocument world)
    {
        var author = source.TryGetValue("AuthorId", out var saved)
            ? ValidateAuthor(saved, world)
            : ResolveLegacyAuthor(source, world);
        var reactions = ReadArray(source, "Reactions").Select(value =>
        {
            var reaction = value.AsBsonDocument;
            var reactionAuthor = reaction.TryGetValue("AuthorId", out var existing)
                ? ValidateAuthor(existing, world)
                : ResolveLegacyAuthor(reaction, world);
            return new BsonDocument { ["AuthorId"] = reactionAuthor, ["Emoji"] = reaction["Emoji"] };
        });
        return new BsonDocument
        {
            ["_id"] = source.GetValue("_id", source.GetValue("Id", BsonNull.Value)),
            ["ParentCommentId"] = source.GetValue("ParentCommentId", BsonNull.Value),
            ["RequestId"] = source.GetValue("RequestId", BsonNull.Value),
            ["AuthorId"] = author,
            ["Text"] = source["Text"],
            ["Hidden"] = source.GetValue("Hidden", false),
            ["Reactions"] = new BsonArray(reactions),
        };
    }

    private static BsonString ResolveLegacyAuthor(BsonDocument source, BsonDocument world)
    {
        if (source.GetValue("Me", false).AsBoolean)
        {
            return Author(AuthorKind.Player, world["Player"].AsBsonDocument["PlayerId"].AsString);
        }

        var stars = ReadArray(world, "Stars").Select(value => value.AsBsonDocument).ToArray();
        var stable = stars.Where(star =>
            source.GetValue("StarId", BsonNull.Value) == ReadStarId(star) ||
            source.GetValue("Avatar", BsonNull.Value) == ReadStarId(star) ||
            source.GetValue("Handle", BsonNull.Value) == $"@{ReadStarId(star)}").ToArray();
        var candidates = stable.Length > 0 ? stable : stars.Where(star => source.GetValue("Author", BsonNull.Value) == star["Name"]).ToArray();
        return candidates is [var resolved] ? Author(AuthorKind.Star, ReadStarId(resolved)) : throw InvalidLegacyContent();
    }

    private static BsonString ValidateAuthor(BsonValue source, BsonDocument world)
    {
        var author = ReadAuthor(source);
        if (author.Kind == AuthorKind.Player && author.Id == world["Player"].AsBsonDocument["PlayerId"].AsString)
        {
            return new BsonString(author.Value);
        }

        if (author.Kind == AuthorKind.Star && ReadArray(world, "Stars").Any(star => ReadStarId(star.AsBsonDocument) == author.Id))
        {
            return new BsonString(author.Value);
        }

        throw InvalidLegacyContent();
    }

    private static AuthorId ReadAuthor(BsonValue source)
    {
        if (source.IsString && AuthorId.TryFrom(source.AsString, out var author))
        {
            return author;
        }

        if (source.IsBsonDocument)
        {
            var document = source.AsBsonDocument;
            if (document.TryGetValue("Kind", out var kind) && kind.IsInt32 &&
                document.TryGetValue("Id", out var id) && id.IsString &&
                kind.AsInt32 is (int)AuthorKind.Player or (int)AuthorKind.Star &&
                !string.IsNullOrWhiteSpace(id.AsString))
            {
                return AuthorId.Create((AuthorKind)kind.AsInt32, id.AsString);
            }
        }

        throw InvalidLegacyContent();
    }

    private static void ValidateBranches(IReadOnlyList<BsonDocument> comments)
    {
        var requests = comments.Select(comment => comment["RequestId"]).Where(value => !value.IsBsonNull).ToArray();
        if (requests.Distinct().Count() != requests.Length)
        {
            throw InvalidLegacyContent();
        }

        foreach (var comment in comments)
        {
            ValidateBranch(comment, comments);
        }
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
            if (player.TryGetValue("PostsUsed", out var used) && player.TryGetValue("EnergyUsed", out var energy) && used != energy)
            {
                throw InvalidLegacyContent();
            }

            var update = Builders<BsonDocument>.Update.Set("EnergyUsed", player.GetValue("EnergyUsed", player.GetValue("PostsUsed", 0)))
                .Set("Version", ReadVersion(player)).Unset("PostsUsed");
            await players.UpdateOneAsync(session, new BsonDocument("_id", player["_id"]), update, cancellationToken: cancellationToken);
        }
    }

    private static void ValidateBranch(BsonDocument comment, IReadOnlyList<BsonDocument> comments)
    {
        var seen = new HashSet<BsonValue> { comment["_id"] };
        var parent = comment["ParentCommentId"];
        while (!parent.IsBsonNull)
        {
            if (!seen.Add(parent))
            {
                throw InvalidLegacyContent();
            }

            parent = (comments.SingleOrDefault(item => item["_id"] == parent) ?? throw InvalidLegacyContent())["ParentCommentId"];
        }
    }

    private static string ReadStarId(BsonDocument star)
    {
        var id = star.GetValue("_id", star.GetValue("Id", BsonNull.Value));
        return id.IsString && !string.IsNullOrWhiteSpace(id.AsString) ? id.AsString : throw InvalidLegacyContent();
    }

    private static BsonValue ReadVersion(BsonDocument document) =>
        document.TryGetValue("Version", out var value) && !value.IsBsonNull ? value : new BsonInt32(0);

    private static BsonArray ReadArray(BsonDocument document, string field) =>
        document.TryGetValue(field, out var value) && !value.IsBsonNull ? value.AsBsonArray : [];

    private static BsonString Author(AuthorKind kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw InvalidLegacyContent();
        }

        return new BsonString(AuthorId.Create(kind, id).Value);
    }

    private static InvalidOperationException InvalidLegacyContent() =>
        new("Legacy content has an unresolved author, world, request or parent. Resolve the source data before rerunning migration 2.2.0.");
}
