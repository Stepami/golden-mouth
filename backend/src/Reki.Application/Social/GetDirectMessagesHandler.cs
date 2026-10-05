using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class GetDirectMessagesHandler(
    IGameStore store,
    GameBalance balance,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<GetDirectMessagesQuery, DirectMessageView>
{
    public async ValueTask<DirectMessageView> Handle(
        GetDirectMessagesQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        DateTimeOffset periodStart = gameDay.PeriodStart;
        StarConversation conversation = await store.GetStarConversationAsync(
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

        int repliesLeft = Math.Max(
            0,
            balance.Relations.DirectMessageRepliesPerDay - conversation.RepliesUsed);
        int relation = player.StarRelations
            .FirstOrDefault(item => item.StarId == request.StarId)?.Value ?? 0;
        return new DirectMessageView(conversation, repliesLeft, relation);
    }
}

