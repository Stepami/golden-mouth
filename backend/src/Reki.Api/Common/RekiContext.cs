using System.Collections.Frozen;
using DotNext.Threading;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Common;

internal sealed class RekiContext : IRekiContext
{
    private readonly AsyncLazy<PlayerProfile> _player;
    private readonly AsyncLazy<IReadOnlyDictionary<WorldId, World>> _worlds;

    public RekiContext(ITelegramUserAccessor telegramUser, IGameStore store)
    {
        _player = new AsyncLazy<PlayerProfile>(
            async ct =>
                await store.GetPlayerAsync(
                    TelegramUserId.From(telegramUser.User.Id),
                    ct) ??
                throw new ResourceNotFoundException("Player not found."),
            resettable: true);
        _worlds = new AsyncLazy<IReadOnlyDictionary<WorldId, World>>(
            async ct =>
            {
                var player = await _player.WithCancellation(ct);
                var worlds = await store.GetWorldsAsync(player.Id, ct);
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
}