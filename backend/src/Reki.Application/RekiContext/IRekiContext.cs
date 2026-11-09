using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.RekiContext;

public interface IRekiContext
{
    Task<PlayerProfile> Player { get; }

    Task<IReadOnlyDictionary<WorldId, World>> Worlds { get; }

    void Reload();
}
