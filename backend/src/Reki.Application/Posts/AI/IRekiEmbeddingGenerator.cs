using Microsoft.Extensions.AI;

namespace Reki.Application.Posts.AI;

public interface IRekiEmbeddingGenerator<in T> :
    IEmbeddingGenerator<T, Embedding<float>>;