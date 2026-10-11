using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Worlds;

public sealed class GetWorldRulesHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetWorldRulesQuery, WorldRules>
{
    public async ValueTask<WorldRules> Handle(
        GetWorldRulesQuery request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        var stored = await store.GetWorldRulesAsync(
            player.Id,
            request.WorldId,
            cancellationToken);
        return request.WorldId.Value == "own" && stored is not null
            ? stored
            : WorldRulesDefaults.Create(player.Id, request.WorldId);
    }
}