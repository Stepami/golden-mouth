using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Reki.Application.Posts.AI;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

internal sealed class StarVectorStore(
    VectorStore vectorStore,
    IRekiEmbeddingGenerator<WorldStar> starEmbedder,
    IRekiEmbeddingGenerator<GamePost> postEmbedder,
    IRekiEmbeddingGenerator<GameComment> commentEmbedder) : IStarVectorStore
{
    private const double AdditionalReplyThreshold = 0.35;

    private readonly VectorStoreCollectionDefinition _definition = new()
    {
        Properties =
        [
            new VectorStoreKeyProperty(
                nameof(StarEntry.Id),
                typeof(StarId)),

            new VectorStoreVectorProperty<WorldStar>(
                nameof(StarEntry.Source),
                dimensions: 312)
            {
                DistanceFunction = DistanceFunction.CosineSimilarity,
                EmbeddingGenerator = starEmbedder,
            },
        ],
    };

    public async Task WarmUpAsync(World world)
    {
        var collection = vectorStore.GetCollection<StarId, StarEntry>(
            world.Id.ToString(),
            _definition);

        await collection.EnsureCollectionDeletedAsync();
        await collection.EnsureCollectionExistsAsync();

        await collection.UpsertAsync(world.Stars.Select(star => new StarEntry
        {
            Id = star.Id,
            Source = star,
        }));
    }

    public async Task<StarId> GetRelevantCommenterAsync(
        World world,
        GamePost post)
    {
        var embedding = await postEmbedder.GenerateVectorAsync(post);

        var collection = vectorStore.GetCollection<StarId, StarEntry>(world.Id.ToString(), _definition);
        if (!await collection.CollectionExistsAsync())
        {
            await WarmUpAsync(world);
        }

        var result = await collection.SearchAsync(embedding, 1).FirstAsync();
        return result.Record.Id;
    }

    public async Task<StarId?> GetRelevantAdditionalReplierAsync(
        World world,
        StarId starToExclude,
        GameComment comment)
    {
        var embedding = await commentEmbedder.GenerateVectorAsync(comment);

        var collection = vectorStore.GetCollection<StarId, StarEntry>(world.Id.ToString(), _definition);
        if (!await collection.CollectionExistsAsync())
        {
            await WarmUpAsync(world);
        }

        var options = new VectorSearchOptions<StarEntry>
        {
            Filter = entry => entry.Id != starToExclude,
            ScoreThreshold = AdditionalReplyThreshold,
        };
        var result = await collection.SearchAsync(embedding, 1, options).FirstOrDefaultAsync();
        return result?.Record.Id;
    }

    private sealed class StarEntry
    {
        public required StarId Id { get; init; }

        public required WorldStar Source { get; init; }
    }
}
