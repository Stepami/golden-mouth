using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Common.AI;

public sealed record StarAgentProfile(
    World World,
    StarId? StarId,
    IReadOnlyList<string> Memory,
    string Address);