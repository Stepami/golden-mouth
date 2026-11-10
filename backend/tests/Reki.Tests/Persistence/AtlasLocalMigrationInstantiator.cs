using Kot.MongoDB.Migrations;
using Kot.MongoDB.Migrations.DI;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Posts;
using Reki.Infrastructure.Mongo.Migrations;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Tests.Persistence;

internal sealed class AtlasLocalMigrationInstantiator(IServiceProvider services) : IMigrationInstantiator
{
    private readonly DIMigrationInstantiator _inner = new(services);

    public IMongoMigration Instantiate(Type migrationType)
    {
        var migration = _inner.Instantiate(migrationType);
        if (migrationType == typeof(CreatePostEmbeddings))
        {
            return new CreatePostEmbeddingsForAtlasLocal(migration);
        }

        return migrationType == typeof(AddPostEmbeddingFilters)
            ? new SkipPostEmbeddingFiltersForAtlasLocal(migration)
            : migration;
    }

    private sealed class CreatePostEmbeddingsForAtlasLocal(IMongoMigration original) : IMongoMigration
    {
        public DatabaseVersion Version => original.Version;

        public string Name => original.Name;

        public async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            var posts = db.GetCollection<GamePost>(MongoCollections.Posts);
            var index = new CreateVectorSearchIndexModel<GamePost>(
                "_embedding",
                "ix_posts_embedding",
                VectorSimilarity.Cosine,
                312,
                new StringFieldDefinition<GamePost>(nameof(GamePost.WorldId)),
                new StringFieldDefinition<GamePost>(nameof(GamePost.AuthorId)));
            await posts.SearchIndexes.CreateOneAsync(index, cancellationToken);
            await original.UpAsync(db, session, cancellationToken);
            await Assert.That(async () =>
                {
                    using var indexes = await posts.SearchIndexes.ListAsync(cancellationToken: cancellationToken);
                    var list = await indexes.ToListAsync(cancellationToken);
                    return list.Any(item => item["name"] == "ix_posts_embedding" &&
                                            item.GetValue("status", string.Empty) == "READY" &&
                                            item.GetValue("queryable", BsonBoolean.False).ToBoolean());
                })
                .WaitsFor(assertion => assertion.IsTrue(), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(1), cancellationToken);
        }

        public Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
            original.DownAsync(db, session, cancellationToken);
    }

    private sealed class SkipPostEmbeddingFiltersForAtlasLocal(IMongoMigration original) : IMongoMigration
    {
        public DatabaseVersion Version => original.Version;

        public string Name => original.Name;

        public Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
            original.DownAsync(db, session, cancellationToken);
    }
}
