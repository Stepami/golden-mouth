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
    private static readonly ProjectionDefinition<GamePost> WithoutEmbedding =
        Builders<GamePost>.Projection.Exclude("_embedding");

    private readonly IMongoCollection<GamePost> _posts =
        database.GetCollection<GamePost>(MongoCollections.Posts);

    public async ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken)
    {
        var embedding = await embeddingGenerator.GenerateVectorAsync(post, cancellationToken: cancellationToken);
        await InsertPostAsync(post, embedding, cancellationToken);
    }

    public async ValueTask InsertPostAsync(GamePost post, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken)
    {
        var document = post.ToBsonDocument();
        document["_embedding"] = new BsonArray(embedding.ToArray());
        var posts = database.GetCollection<BsonDocument>(MongoCollections.Posts);
        await (sessionProvider.Current is { } session
            ? posts.InsertOneAsync(session, document, cancellationToken: cancellationToken)
            : posts.InsertOneAsync(document, cancellationToken: cancellationToken));
    }

    public async ValueTask<IReadOnlyList<PostSimilarity>> FindSimilarPostsAsync(
        WorldId worldId,
        AuthorId authorId,
        ReadOnlyMemory<float> embedding,
        double threshold,
        CancellationToken cancellationToken)
    {
        var filter = Builders<GamePost>.Filter.Eq(item => item.WorldId, worldId) &
                     Builders<GamePost>.Filter.Eq(item => item.AuthorId, authorId) &
                     Builders<GamePost>.Filter.Exists("_embedding", true);
        var match = filter.Render(new RenderArgs<GamePost>(_posts.DocumentSerializer, _posts.Settings.SerializerRegistry));
        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            new BsonDocument("$set", new BsonDocument("score", new BsonDocument(
                "$similarityCosine", new BsonArray { "$_embedding", new BsonArray(embedding.ToArray()) }))),
            new BsonDocument("$match", new BsonDocument("score", new BsonDocument("$gte", threshold))),
            new BsonDocument("$project", new BsonDocument { { "Text", 1 }, { "score", 1 } }),
        };
        using var cursor = await database.GetCollection<BsonDocument>(MongoCollections.Posts)
            .AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);
        return documents.Select(item => new PostSimilarity(
            PostId.From(item["_id"].AsGuid), item["Text"].AsString, item["score"].ToDouble())).ToArray();
    }

    public async ValueTask<WorldPostsPage> GetPostsAsync(
        WorldId worldId,
        AuthorId? authorId,
        PostId? cursor,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var limit = pageSize + 1;
        var filter = Builders<GamePost>.Filter.Eq(item => item.WorldId, worldId);
        if (authorId is { } author)
        {
            filter &= Builders<GamePost>.Filter.Eq(item => item.AuthorId, author);
        }

        if (cursor is { } before)
        {
            filter &= Builders<GamePost>.Filter.Lt(item => item.Id, before);
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
        WorldId worldId,
        CancellationToken cancellationToken) =>
        await _posts.Find(item => item.Id == postId && item.WorldId == worldId)
            .Project<GamePost>(WithoutEmbedding)
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<GamePost?> GetPublicStoryPostAsync(PostId postId, CancellationToken cancellationToken) =>
        await _posts.Find(item => item.Id == postId)
            .Project<GamePost>(WithoutEmbedding)
            .FirstOrDefaultAsync(cancellationToken);

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
}
