using Reki.Application.Exceptions;
using Reki.Application.RekiContext;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.RekiContext;

internal sealed class NoPlayerRekiContext : IRekiContext
{
    public Task<PlayerProfile> Player { get; } =
        Task.FromException<PlayerProfile>(
            new ResourceNotFoundException("Player not found."));

    public Task<IReadOnlyDictionary<WorldId, World>> Worlds { get; } =
        Task.FromException<IReadOnlyDictionary<WorldId, World>>(
            new ResourceNotFoundException("Player not found."));

    public void Reload() => throw new NotSupportedException();
}
