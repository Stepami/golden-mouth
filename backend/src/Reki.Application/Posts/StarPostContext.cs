namespace Reki.Application.Posts;

public sealed record StarPostContext(
    string Text,
    string World,
    string WorldRules,
    string Voice,
    string Stage,
    IReadOnlyList<string> RecentPosts,
    bool HateEnabled);