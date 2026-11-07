using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public interface IStarVectorStore
{
    Task WarmUpAsync(World world);

    Task<StarId> GetRelevantCommenterAsync(
        World world,
        GamePost post);

    Task<StarId> GetRelevantCommenterAsync(World world, ReadOnlyMemory<float> embedding);

    Task<StarId?> GetRelevantAdditionalReplierAsync(
        World world,
        StarId starToExclude,
        GameComment comment);
}
