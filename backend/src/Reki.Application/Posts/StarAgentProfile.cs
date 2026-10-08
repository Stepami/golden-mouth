using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record StarAgentProfile(
    WorldId WorldId,
    string WorldTitle,
    string WorldDescription,
    IReadOnlyList<WorldRule> WorldRules,
    string Drama,
    string Voice,
    StarId? StarId,
    IReadOnlyList<string> Memory,
    string Address);
