using MongoDB.Driver;
using Reki.Application.Posts;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPostRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider) : IPostRepository
{
    private readonly IMongoCollection<GamePost> _posts =
        database.GetCollection<GamePost>(MongoCollections.Posts);

    public async ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken) =>
        await (sessionProvider.Current is { } session
            ? _posts.InsertOneAsync(session, post, cancellationToken: cancellationToken)
            : _posts.InsertOneAsync(post, cancellationToken: cancellationToken));

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

    public async ValueTask SaveReplyAsync(GamePost post, CancellationToken cancellationToken) =>
        await _posts.ReplaceOneAsync(
            sessionProvider, item => item.Id == post.Id && item.WorldId == post.WorldId, post, cancellationToken);

    public async ValueTask DeletePostsAsync(IReadOnlyCollection<WorldId> worldIds, CancellationToken cancellationToken) =>
        await _posts.DeleteManyAsync(
            sessionProvider.Session,
            Builders<GamePost>.Filter.In(item => item.WorldId, worldIds),
            cancellationToken: cancellationToken);
}
