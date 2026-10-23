using Reki.Application.Balance;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record PublicationBalance(WorldDifficulty Difficulty, DifficultyBalance Coefficients);
