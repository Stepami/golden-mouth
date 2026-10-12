using Reki.Domain.Posts;
using Reki.Domain.Social;

namespace Reki.Application.Posts;

public sealed record StarReplyContext(
    StarAgentProfile Agent,
    StarId TargetStarId,
    string PostText,
    IReadOnlyList<GameComment> Branch,
    string PlayerText,
    bool HateEnabled);