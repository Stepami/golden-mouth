using System.Collections.Frozen;
using System.Security.Claims;
using DotNext.Threading;
using Microsoft.AspNetCore.Http;
using Reki.Application.Exceptions;
using Reki.Application.Player;
using Reki.Application.RekiContext;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.RekiContext;

internal sealed class LazyRekiContext : IRekiContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AsyncLazy<PlayerProfile> _player;
    private readonly AsyncLazy<IReadOnlyDictionary<WorldId, World>> _worlds;

    public LazyRekiContext(
        IHttpContextAccessor httpContextAccessor,
        IPlayerRepository playerRepository,
        IWorldPlayerCache worldCache)
    {
        _httpContextAccessor = httpContextAccessor;
        _player = new AsyncLazy<PlayerProfile>(
            async ct =>
                await playerRepository.GetPlayerAsync(PlayerId, ct) ??
                throw new ResourceNotFoundException("Player not found."),
            resettable: true);
        _worlds = new AsyncLazy<IReadOnlyDictionary<WorldId, World>>(
            async ct =>
            {
                var worlds = await worldCache.GetWorldsAsync(PlayerId, ct);
                return worlds.ToFrozenDictionary(world => world.Id);
            },
            resettable: true);
    }

    public PlayerId PlayerId => PlayerId.From(
        _httpContextAccessor.HttpContext?.User.FindFirstValue(OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject)
        ?? throw new InvalidOperationException("Player subject is missing."));

    public Task<PlayerProfile> Player =>
        _player.WithCancellation(CancellationToken.None);

    public Task<IReadOnlyDictionary<WorldId, World>> Worlds =>
        _worlds.WithCancellation(CancellationToken.None);

    public void Reload()
    {
        _player.Reset();
        _worlds.Reset();
    }
}
