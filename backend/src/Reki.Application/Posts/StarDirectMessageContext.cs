using Reki.Domain.Social;

namespace Reki.Application.Posts;

public sealed record StarDirectMessageContext(
    StarAgentProfile Agent,
    string Text)
{
    public IReadOnlyList<DirectMessage> History { get; init; } = [];
}
