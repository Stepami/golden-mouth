namespace Reki.Application.Posts;

public sealed record StarPostContext(
    string Text,
    StarAgentProfile Agent,
    string Stage,
    IReadOnlyList<string> RecentPosts,
    bool HateEnabled);
