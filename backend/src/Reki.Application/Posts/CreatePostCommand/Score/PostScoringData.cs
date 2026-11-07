using Reki.Application.Balance;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Score;

internal sealed record PostScoringData(
    World World,
    GameBalance Balance,
    ReadOnlyMemory<float> Embedding,
    CancellationToken CancellationToken);
