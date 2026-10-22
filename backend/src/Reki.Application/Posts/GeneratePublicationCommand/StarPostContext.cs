using Reki.Application.Common.AI;

namespace Reki.Application.Posts.GeneratePublicationCommand;

public sealed record StarPostContext(
    string Text,
    StarAgentProfile Agent,
    string Stage,
    IReadOnlyList<string> RecentPosts,
    bool HateEnabled);
