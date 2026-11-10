using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.RekiContext;

public interface IRekiContext
{
    PlayerId PlayerId { get; }

    Task<PlayerProfile> Player { get; }

    Task<IReadOnlyDictionary<WorldId, World>> Worlds { get; }

    void Reload();
}
