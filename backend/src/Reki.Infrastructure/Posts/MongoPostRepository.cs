using MongoDB.Driver;
using Reki.Application.Posts;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Persistence;

namespace Reki.Infrastructure.Posts;

internal sealed class MongoPostRepository : IPostRepository
{
    private readonly IMongoCollection<GamePost> _posts;

    public MongoPostRepository(MongoCompositionRoot compositionRoot)
    {
        _posts = compositionRoot.MongoDatabase.GetCollection<GamePost>(MongoCollections.Posts);
    }

    public async ValueTask InsertPostAsync(GamePost post, CancellationToken cancellationToken) =>
        await _posts.InsertOneAsync(post, cancellationToken: cancellationToken);

    public async ValueTask<WorldPostsPage> GetPostsAsync(
        WorldId worldId,
        AuthorId? authorId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var offset = pageNumber * pageSize;
        var limit = pageSize + 1;
        var filter = Builders<GamePost>.Filter.Eq(item => item.WorldId, worldId);
        if (authorId is { } author)
        {
            filter &= Builders<GamePost>.Filter.Eq(item => item.AuthorId, author);
        }

        var posts = await _posts
            .Find(filter)
            .SortByDescending(item => item.CreatedAtUtc)
            .Skip(offset)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        var hasNext = posts.Count > pageSize;
        if (hasNext)
        {
            posts.RemoveAt(posts.Count - 1);
        }

        return new WorldPostsPage(posts, pageNumber, hasNext);
    }

    public async ValueTask<GamePost?> GetPostAsync(
        PostId postId,
        WorldId worldId,
        CancellationToken cancellationToken) =>
        await _posts.Find(item => item.Id == postId && item.WorldId == worldId)
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<GamePost?> GetPublicStoryPostAsync(PostId postId, CancellationToken cancellationToken) =>
        await _posts.Find(item => item.Id == postId).FirstOrDefaultAsync(cancellationToken);
}
