using Microsoft.Extensions.AI;
using Reki.Application.Posts.AI;

namespace Reki.Infrastructure.AI.Embedding;

internal sealed class RekiEmbeddingGenerator<T>(
    IEncodeSerializer<T> serializer,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) :
    IRekiEmbeddingGenerator<T>
{
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<T> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var serializedValues = values.Select(serializer.Serialize);
        return embeddingGenerator.GenerateAsync(serializedValues, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        embeddingGenerator.GetService(serviceType, serviceKey);

    public void Dispose() => embeddingGenerator.Dispose();
}