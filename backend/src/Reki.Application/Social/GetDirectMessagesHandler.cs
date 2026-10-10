using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Social;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Social;

public sealed class GetDirectMessagesHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    IOptions<GameBalance> balance,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<GetDirectMessagesQuery, DirectMessageView>
{
    public async ValueTask<DirectMessageView> Handle(
        GetDirectMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        _ = StarCatalog.Require(player.World, request.StarId);
        var periodStart = gameDay.PeriodStart;
        var conversation = await store.GetStarConversationAsync(
            player.Id,
            request.StarId,
            cancellationToken) ?? new StarConversation(
                StarConversationId.New(),
                player.Id,
                request.StarId,
                [],
                [],
                0,
                periodStart);
        if (conversation.PeriodStartedAtUtc != periodStart)
        {
            conversation = conversation with { RepliesUsed = 0, PeriodStartedAtUtc = periodStart };
            await store.SaveStarConversationAsync(conversation, cancellationToken);
        }

        var repliesLeft = Math.Max(
            0,
            balance.Value.Relations.DirectMessageRepliesPerDay - conversation.RepliesUsed);
        var relation = player.StarRelations
            .FirstOrDefault(item => item.StarId == request.StarId)?.Value ?? 0;
        return new DirectMessageView(conversation, repliesLeft, relation, StarCatalog.Require(player.World, request.StarId));
    }
}

