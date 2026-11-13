using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Posts;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPostRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider,
    IRekiEmbeddingGenerator<GamePost> embeddingGenerator) : IPostRepository
{
    private const int SimilarityLimit = 20;
    private const int SimilarityCandidates = 400;
    private const string EmbeddingIndex = "ix_posts_embedding";

    private static readonly ProjectionDefinition<GamePost> WithoutEmbedding =
        Builders<GamePost>.Projection.Exclude("_embedding");

    private readonly IMongoCollection<GamePost> _posts =
        database.GetCollection<GamePost>(MongoCollections.Posts);

    public async ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken)
    {
        var embedding = await embeddingGenerator.GenerateVectorAsync(post, cancellationToken: cancellationToken);
        var document = post.ToBsonDocument();
        document["_embedding"] = new BsonArray(embedding.ToArray());
        var posts = database.GetCollection<BsonDocument>(MongoCollections.Posts);
        await (sessionProvider.Current is { } session
            ? posts.InsertOneAsync(session, document, cancellationToken: cancellationToken)
            : posts.InsertOneAsync(document, cancellationToken: cancellationToken));
    }

    public async ValueTask<IReadOnlyList<PostSimilarity>> FindSimilarPostsAsync(
        GamePost post,
        double threshold)
    {
        var embedding = await embeddingGenerator.GenerateVectorAsync(post);
        var filter = Builders<GamePost>.Filter.Eq(item => item.WorldId, post.WorldId) &
                     Builders<GamePost>.Filter.Eq(item => item.AuthorId, post.AuthorId);
        var match = filter.Render(new RenderArgs<GamePost>(_posts.DocumentSerializer, _posts.Settings.SerializerRegistry));
        var score = new BsonDocument("$subtract", new BsonArray
        {
            new BsonDocument("$multiply", new BsonArray { new BsonDocument("$meta", "vectorSearchScore"), 2 }),
            1,
        });
        var pipeline = new[]
        {
            new BsonDocument("$vectorSearch", new BsonDocument
            {
                { "index", EmbeddingIndex },
                { "path", "_embedding" },
                { "queryVector", new BsonArray(embedding.ToArray()) },
                { "numCandidates", SimilarityCandidates },
                { "limit", SimilarityLimit },
                { "filter", match },
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "Text", 1 },
                { "score", score },
            }),
            new BsonDocument("$match", new BsonDocument("score", new BsonDocument("$gte", threshold))),
        };
        using var cursor = await database
            .GetCollection<BsonDocument>(MongoCollections.Posts)
            .AggregateAsync<BsonDocument>(pipeline);
        var documents = await cursor.ToListAsync();
        return documents.Select(item => new PostSimilarity(
            PostId.From(item["_id"].AsGuid), item["Text"].AsString, item["score"].ToDouble())).ToArray();
    }

    public async ValueTask<WorldPostsPage> GetPostsAsync(
        WorldId worldId,
        IReadOnlyCollection<AuthorId> authorIds,
        PostId? cursor,
        int pageSize,
        AuthorId? likedBy,
        CancellationToken cancellationToken)
    {
        var limit = pageSize + 1;
        var filter = Builders<GamePost>.Filter.Eq(item => item.WorldId, worldId);
        if (authorIds.Count > 0)
        {
            filter &= Builders<GamePost>.Filter.In(item => item.AuthorId, authorIds);
        }

        if (cursor is { } before)
        {
            filter &= Builders<GamePost>.Filter.Lt(item => item.Id, before);
        }

        if (likedBy is { } liker)
        {
            filter &= Builders<GamePost>.Filter.AnyEq(item => item.LikedBy, liker);
        }

        var posts = await _posts
            .Find(filter)
            .SortByDescending(item => item.Id)
            .Limit(limit)
            .Project<GamePost>(WithoutEmbedding)
            .ToListAsync(cancellationToken);
        var hasNext = posts.Count > pageSize;
        if (hasNext)
        {
            posts.RemoveAt(posts.Count - 1);
        }

        return new WorldPostsPage(posts, hasNext ? posts[^1].Id : null);
    }

    public async ValueTask<GamePost?> GetPostAsync(
        PostId postId,
        CancellationToken cancellationToken) =>
        await _posts.Find(item => item.Id == postId)
            .Project<GamePost>(WithoutEmbedding)
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask LikePostAsync(PostId postId, AuthorId authorId, bool like, CancellationToken cancellationToken)
    {
        var filter = Builders<GamePost>.Filter.Where(item => item.Id == postId);
        var update = like
            ? Builders<GamePost>.Update.AddToSet(item => item.LikedBy, authorId)
            : Builders<GamePost>.Update.Pull(item => item.LikedBy, authorId);
        await (sessionProvider.Current is { } session
            ? _posts.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken)
            : _posts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken));
    }

    public async ValueTask SaveReplyAsync(GamePost post, CancellationToken cancellationToken)
    {
        var filter = Builders<GamePost>.Filter.Where(item => item.Id == post.Id && item.WorldId == post.WorldId);
        var update = Builders<GamePost>.Update
            .Set(item => item.Comments, post.Comments)
            .Set(item => item.Version, post.Version);
        await (sessionProvider.Current is { } session
            ? _posts.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken)
            : _posts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken));
    }

    public async ValueTask DeletePostsAsync(IReadOnlyCollection<WorldId> worldIds, CancellationToken cancellationToken) =>
        await _posts.DeleteManyAsync(
            sessionProvider.Session,
            Builders<GamePost>.Filter.In(item => item.WorldId, worldIds),
            cancellationToken: cancellationToken);

    public async ValueTask DeletePostAsync(PostId postId, CancellationToken cancellationToken) =>
        await (sessionProvider.Current is { } session
            ? _posts.DeleteOneAsync(
                session,
                post => post.Id == postId,
                cancellationToken: cancellationToken)
            : _posts.DeleteOneAsync(
                post => post.Id == postId,
                cancellationToken));
}
