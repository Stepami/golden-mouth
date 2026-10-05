using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Posts;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class SendDirectMessageHandler(
    IGameStore store,
    GameBalance balance,
    GameDay gameDay,
    PlayerReader playerReader,
    PlayerSafetyGuard safetyGuard,
    StarTurnGenerator turnGenerator) : IRequestHandler<SendDirectMessageCommand, SentDirectMessage>
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

        await safetyGuard.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
        GeneratedDirectMessage generated = await turnGenerator.GenerateDirectMessageAsync(
            new StarDirectMessageContext(
                request.StarId,
                player.World,
                player.Settings.Voice,
                conversation.Memory,
                request.Text),
            cancellationToken);
        IReadOnlyList<DirectMessage> messages =
        [
            .. conversation.Messages,
            new DirectMessage("me", request.Text),
            new DirectMessage("star", generated.Text),
        ];
        IReadOnlyList<string> memory = string.IsNullOrWhiteSpace(generated.MemoryFact) ||
            conversation.Memory.Contains(generated.MemoryFact, StringComparer.OrdinalIgnoreCase)
            ? conversation.Memory
            : [.. conversation.Memory, generated.MemoryFact];
        StarConversation updated = conversation with
        {
            Messages = messages,
            Memory = memory,
            RepliesUsed = conversation.RepliesUsed + 1,
        };
        await store.SaveStarConversationAsync(updated, cancellationToken);
        return new SentDirectMessage(
            generated.Text,
            balance.DirectMessageRepliesPerDay - updated.RepliesUsed);
    }
}