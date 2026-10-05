using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class SendDirectMessageHandler(
    IGameStore store,
    GameBalance balance,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<SendDirectMessageCommand, SentDirectMessage>
{
    public async ValueTask<SentDirectMessage> Handle(
        SendDirectMessageCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        DateTimeOffset periodStart = gameDay.PeriodStart;
        StarConversation conversation = await store.GetStarConversationAsync(
            player.Id,
            request.StarId,
            cancellationToken) ?? new StarConversation(
                player.Id,
                request.StarId,
                [],
                [],
                0,
                periodStart);
        if (conversation.PeriodStartedAtUtc != periodStart)
        {
            conversation = conversation with { RepliesUsed = 0, PeriodStartedAtUtc = periodStart };
        }

        if (conversation.RepliesUsed >= balance.DirectMessageRepliesPerDay)
        {
            throw new RequestConflictException("The daily direct-message limit has been reached.");
        }

        const string Reply = "Вижу, о чём ты. Давай вернёмся к этому после следующего поста.";
        IReadOnlyList<DirectMessage> messages =
        [
            .. conversation.Messages,
            new DirectMessage("me", request.Text),
            new DirectMessage("star", Reply),
        ];
        StarConversation updated = conversation with
        {
            Messages = messages,
            RepliesUsed = conversation.RepliesUsed + 1,
        };
        await store.SaveStarConversationAsync(updated, cancellationToken);
        return new SentDirectMessage(
            Reply,
            balance.DirectMessageRepliesPerDay - updated.RepliesUsed);
    }
}

