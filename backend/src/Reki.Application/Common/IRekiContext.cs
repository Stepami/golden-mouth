using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Common;

public interface IRekiContext
{
    Task<PlayerProfile> Player { get; }

    Task<IReadOnlyDictionary<WorldId, World>> Worlds { get; }

    void Reload();
}