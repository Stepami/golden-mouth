using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class SendDirectMessageHandler(
    IGameStore store,
    IOptions<GameBalance> balance,
    GameDayPeriod gameDayPeriod,
    ISender sender,
    StarTurnGenerator turnGenerator) : IRequestHandler<SendDirectMessageCommand, SentDirectMessage>
{
    public async ValueTask<SentDirectMessage> Handle(
        SendDirectMessageCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var world = await sender.Send(new Worlds.GetWorldQuery(player.Id, request.WorldId), cancellationToken);
        _ = world.FindStar(request.StarId) ?? throw new ResourceNotFoundException("The star does not belong to this world.");
        var periodStart = gameDayPeriod.PeriodStart;
        var conversation = await store.GetStarConversationAsync(
            player.Id,
            request.StarId,
            cancellationToken,
            world.Id) ?? new StarConversation(
                StarConversationId.New(),
                player.Id,
                world.Id,
                request.StarId,
                [],
                [],
                0,
                periodStart);
        if (conversation.PeriodStartedAtUtc != periodStart)
        {
            conversation = conversation with { RepliesUsed = 0, PeriodStartedAtUtc = periodStart };
        }

        if (conversation.RepliesUsed >= balance.Value.Relations.DirectMessageRepliesPerDay)
        {
            throw new RequestConflictException("The daily direct-message limit has been reached.");
        }

        var generated = await turnGenerator.GenerateDirectMessageAsync(
            new StarDirectMessageContext(
                new StarAgentProfile(world, request.StarId, conversation.Memory, player.Address),
                request.Text,
                conversation.Messages.TakeLast(12).ToArray()),
            player,
            cancellationToken);
        IReadOnlyList<DirectMessage> messages =
        [
            .. conversation.Messages,
            new DirectMessage("me", request.Text),
            new DirectMessage("star", generated.Text),
        ];
        var memory = !MemoryFactPolicy.IsAllowed(generated.MemoryFact) ||
                     conversation.Memory.Contains(generated.MemoryFact, StringComparer.OrdinalIgnoreCase)
            ? conversation.Memory
            : [.. conversation.Memory, generated.MemoryFact];
        var updated = conversation with
        {
            Messages = messages,
            Memory = memory,
            RepliesUsed = conversation.RepliesUsed + 1,
        };
        await store.SaveStarConversationAsync(updated, cancellationToken);
        return new SentDirectMessage(
            generated.Text,
            balance.Value.Relations.DirectMessageRepliesPerDay - updated.RepliesUsed);
    }
}