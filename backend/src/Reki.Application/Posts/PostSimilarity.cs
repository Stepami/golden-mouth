using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record PostSimilarity(PostId Id, string Text, double Score);
