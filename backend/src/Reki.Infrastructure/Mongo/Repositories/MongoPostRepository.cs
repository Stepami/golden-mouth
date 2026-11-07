using MongoDB.Driver;
using Reki.Application.Posts;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPostRepository : IPostRepository
{
    private readonly IMongoCollection<GamePost> _posts;
    private readonly MongoSessionProvider _sessionProvider;

    public MongoPostRepository(IMongoDatabase database, MongoSessionProvider sessionProvider)
    {
        _posts = database.GetCollection<GamePost>(MongoCollections.Posts);
        _sessionProvider = sessionProvider;
    }

    public async ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken) =>
        await _posts.InsertOneAsync(_sessionProvider, post, cancellationToken);

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
            .Find(_sessionProvider, filter)
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
        await _posts.Find(_sessionProvider, item => item.Id == postId && item.WorldId == worldId)
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<GamePost?> GetPublicStoryPostAsync(PostId postId, CancellationToken cancellationToken) =>
        await _posts.Find(_sessionProvider, item => item.Id == postId).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<bool> TrySaveReplyAsync(
        GamePost post,
        int? expectedVersion,
        string? requestId,
        CancellationToken cancellationToken)
    {
        var filter = Builders<GamePost>.Filter.And(
            Builders<GamePost>.Filter.Eq(item => item.Id, post.Id),
            Builders<GamePost>.Filter.Eq(item => item.WorldId, post.WorldId));
        if (expectedVersion is { } version)
        {
            filter &= Builders<GamePost>.Filter.Eq(item => item.Version, version);
        }

        if (requestId is not null)
        {
            filter &= Builders<GamePost>.Filter.Not(
                Builders<GamePost>.Filter.ElemMatch(item => item.Comments, comment => comment.RequestId == requestId));
        }

        var result = await _posts.ReplaceOneAsync(_sessionProvider, filter, post, cancellationToken);
        return result.MatchedCount == 1;
    }

    public async ValueTask DeletePostsAsync(IReadOnlyCollection<WorldId> worldIds, CancellationToken cancellationToken) =>
        await _posts.DeleteManyAsync(
            _sessionProvider.Session,
            Builders<GamePost>.Filter.In(item => item.WorldId, worldIds),
            cancellationToken: cancellationToken);
}
