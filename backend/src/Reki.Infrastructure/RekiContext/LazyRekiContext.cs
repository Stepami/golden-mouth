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
                await playerRepository.GetPlayerAsync(
                    TelegramUserId,
                    ct) ??
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

    public TelegramUserId TelegramUserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User.FindFirstValue(AuthConstants.Claims.TelegramUserId);
            return TelegramUserId.From(long.Parse(
                id ?? throw new InvalidOperationException("Telegram user claim is missing."),
                System.Globalization.CultureInfo.InvariantCulture));
        }
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
}
