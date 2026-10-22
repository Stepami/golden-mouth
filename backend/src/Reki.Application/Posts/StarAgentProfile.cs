using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record StarAgentProfile(
    World World,
    StarId? StarId,
    string Address);
