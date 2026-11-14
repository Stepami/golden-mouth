using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Reki.Infrastructure.AI.Embedding;

internal sealed class RekiOpenAiEmbedder(
    IOptions<EmbeddingOptions> options,
    IMemoryCache cache) :
    OpenAIClient(
        new ApiKeyCredential("local"),
        new OpenAIClientOptions { Endpoint = new Uri(options.Value.BaseUrl) })
{
    public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator() =>
        GetEmbeddingClient(options.Value.Model)
            .AsIEmbeddingGenerator().AsBuilder()
            .Use(inner => new MemoryCachingEmbeddingGenerator(cache, inner))
            .Build();
}