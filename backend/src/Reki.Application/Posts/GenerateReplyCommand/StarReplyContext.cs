using Reki.Application.Posts.AI;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GenerateReplyCommand;

public sealed record StarReplyContext(
    StarAgentProfile Agent,
    StarId TargetStarId,
    string PostText,
    IReadOnlyList<GameComment> Branch,
    string PlayerText,
    bool HateEnabled,
    bool Additional = false);
