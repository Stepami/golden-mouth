using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI;

public sealed record StarAgentProfile(
    World World,
    StarId? StarId,
    string Address);
