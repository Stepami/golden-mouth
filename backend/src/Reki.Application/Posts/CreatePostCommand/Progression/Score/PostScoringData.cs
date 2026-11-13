using Reki.Application.Balance;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score;

internal sealed record PostScoringData(
    World World,
    GameBalance Balance);
