using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Exceptions;
using Reki.Application.Posts;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPostRepository : IPostRepository
{
    private readonly IMongoCollection<GamePost> _posts;

    public MongoPostRepository(IMongoDatabase database)
    {
        _posts = database.GetCollection<GamePost>(MongoCollections.Posts);
    }

    public async IAsyncEnumerable<GamePost> ScanPostsAsync(
        string revision,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filter = Builders<GamePost>.Filter.Eq(item => item.Embedding, null) |
                     Builders<GamePost>.Filter.Ne(item => item.EmbeddingRevision, revision);
        filter &= Builders<GamePost>.Filter.Regex(item => item.AuthorId, new BsonRegularExpression("^Player_"));
        await foreach (var post in _posts.Find(filter).ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return post;
        }
    }

    public async ValueTask<bool> TrySetEmbeddingAsync(
        GamePost post,
        float[] embedding,
        string revision,
        string textHash,
        CancellationToken cancellationToken)
    {
        var update = Builders<GamePost>.Update
            .Set(item => item.Embedding, embedding)
            .Set(item => item.EmbeddingRevision, revision)
            .Set(item => item.EmbeddingTextHash, textHash)
            .Inc(item => item.Version, 1);
        var result = await _posts.UpdateOneAsync(
            item => item.Id == post.Id && item.WorldId == post.WorldId && item.Version == post.Version,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken) =>
        await _posts.InsertOneAsync(post, cancellationToken: cancellationToken);

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
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<GamePost?> GetPublicStoryPostAsync(PostId postId, CancellationToken cancellationToken) =>
        await _posts.Find(item => item.Id == postId).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask SaveReplyAsync(GamePost post, CancellationToken cancellationToken)
    {
        var result = await _posts.ReplaceOneAsync(
            item => item.Id == post.Id && item.WorldId == post.WorldId && item.Version == post.Version - 1,
            post,
            cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
        {
            throw new RequestConflictException("Post changed concurrently.");
        }
    }
}
