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
using Reki.Infrastructure.Auth;

namespace Reki.Infrastructure.RekiContext;

internal sealed class LazyRekiContext : IRekiContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AsyncLazy<PlayerProfile> _player;
    private readonly AsyncLazy<IReadOnlyDictionary<WorldId, World>> _worlds;

    public LazyRekiContext(
        IHttpContextAccessor httpContextAccessor,
        IPlayerRepository playerRepository,
        IWorldRepository worldRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _player = new AsyncLazy<PlayerProfile>(
            async ct =>
                await ResolvePlayerAsync(playerRepository, ct) ??
                throw new ResourceNotFoundException("Player not found."),
            resettable: true);
        _worlds = new AsyncLazy<IReadOnlyDictionary<WorldId, World>>(
            async ct =>
            {
                var player = await _player.WithCancellation(ct);
                var worlds = await worldRepository.GetWorldsAsync(player.Id, ct);
                return worlds.ToFrozenDictionary(world => world.Id);
            },
            resettable: true);
    }

    public Task<PlayerProfile> Player =>
        _player.WithCancellation(CancellationToken.None);

    public Task<IReadOnlyDictionary<WorldId, World>> Worlds =>
        _worlds.WithCancellation(CancellationToken.None);

    public void Reload()
    {
        _player.Reset();
        _worlds.Reset();
    }

    private async ValueTask<PlayerProfile?> ResolvePlayerAsync(IPlayerRepository repository, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var legacyId = user?.FindFirstValue(AuthConstants.Claims.TelegramUserId);
        if (legacyId is not null)
        {
            return await repository.GetPlayerAsync(ExternalUserId.From($"telegram:{legacyId}"), cancellationToken);
        }

        var subject = user?.FindFirstValue(OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject)
            ?? throw new InvalidOperationException("Player subject is missing.");
        return await repository.GetPlayerAsync(PlayerId.From(subject), cancellationToken);
    }
}
