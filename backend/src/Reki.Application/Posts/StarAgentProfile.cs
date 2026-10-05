using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record StarAgentProfile(
    string WorldId,
    string WorldTitle,
    string WorldDescription,
    IReadOnlyList<WorldRule> WorldRules,
    string Drama,
    string Voice,
    string StarId,
    IReadOnlyList<string> Memory);
