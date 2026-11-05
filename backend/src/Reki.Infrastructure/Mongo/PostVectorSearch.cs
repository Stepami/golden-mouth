using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Posts;
using Reki.Infrastructure.AI;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo;

internal sealed class PostVectorSearch(IMongoDatabase database, IOptions<EmbeddingOptions> options) : IHostedService, IHealthCheck
{
    private const string IndexName = "post_embedding_hnsw";
    private readonly IMongoCollection<GamePost> _posts = database.GetCollection<GamePost>(MongoCollections.Posts);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var cursor = await _posts.SearchIndexes.ListAsync(IndexName, cancellationToken: cancellationToken);
        if ((await cursor.ToListAsync(cancellationToken)).Count != 0)
        {
            return;
        }

        var definition = new BsonDocument("fields", new BsonArray
        {
            new BsonDocument
            {
                { "type", "vector" },
                { "path", nameof(GamePost.Embedding) },
                { "numDimensions", 312 },
                { "similarity", "dotProduct" },
                { "indexingMethod", "hnsw" },
            },
            new BsonDocument { { "type", "filter" }, { "path", nameof(GamePost.WorldId) } },
            new BsonDocument { { "type", "filter" }, { "path", nameof(GamePost.AuthorId) } },
            new BsonDocument { { "type", "filter" }, { "path", nameof(GamePost.EmbeddingRevision) } },
        });
        await _posts.SearchIndexes.CreateOneAsync(
            new CreateSearchIndexModel(IndexName, SearchIndexType.VectorSearch, definition),
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
        {
            return HealthCheckResult.Healthy();
        }

        try
        {
            using var cursor = await _posts.SearchIndexes.ListAsync(IndexName, cancellationToken: cancellationToken);
            var index = (await cursor.ToListAsync(cancellationToken)).SingleOrDefault();
            return index is not null && index.GetValue("status", string.Empty).AsString == "READY" &&
                   index.GetValue("queryable", false).AsBoolean
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The post vector index is not ready.");
        }
        catch (MongoException)
        {
            return HealthCheckResult.Unhealthy("The post vector index is unavailable.");
        }
    }
}
