using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Social;

public sealed class ForgetStarMemoryHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store, PlayerReader playerReader)
    : IRequestHandler<ForgetStarMemoryCommand>
{
    public async ValueTask<Unit> Handle(
        ForgetStarMemoryCommand request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        _ = StarCatalog.Require(player.World, request.StarId);
        var conversation = await store.GetStarConversationAsync(
            player.Id,
            request.StarId,
            cancellationToken);
        if (conversation is not null)
        {
            await store.SaveStarConversationAsync(
                conversation with
                {
                    Memory = conversation.Memory
                        .Where(item => !string.Equals(item, request.Fact, StringComparison.Ordinal))
                        .ToArray(),
                },
                cancellationToken);
        }

        return Unit.Value;
    }
}

