using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Posts;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class SendDirectMessageHandler(
    IStarConversationRepository starConversationRepository,
    IOptions<GameBalance> balance,
    GameDayPeriod gameDayPeriod,
    IRekiContextAccessor rekiContextAccessor,
    StarTurnGenerator turnGenerator,
    PlayerSafetyGuard safety) : IRequestHandler<SendDirectMessageCommand, SentDirectMessage>
{
    public async ValueTask<SentDirectMessage> Handle(
        SendDirectMessageCommand request,
        CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        _ = world.FindStar(request.StarId) ?? throw new ResourceNotFoundException("The star does not belong to this world.");
        var periodStart = gameDayPeriod.PeriodStart;
        var conversation = await starConversationRepository.GetStarConversationAsync(
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

        await safety.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
        var generated = await turnGenerator.GenerateDirectMessageAsync(
            new StarDirectMessageContext(
                new StarAgentProfile(world, request.StarId, conversation.Memory, player.Address),
                request.Text,
                conversation.Messages.TakeLast(12).ToArray()),
            cancellationToken);
        rekiContext.Reload();
        player = await rekiContext.Player;
        world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        _ = world.FindStar(request.StarId) ?? throw new ResourceNotFoundException("The star does not belong to this world.");
        var current = await starConversationRepository.GetStarConversationAsync(
            player.Id, request.StarId, cancellationToken, world.Id) ?? conversation;
        if (!current.Messages.SequenceEqual(conversation.Messages) || !current.Memory.SequenceEqual(conversation.Memory))
        {
            throw new RequestConflictException("The conversation changed during generation.");
        }

        periodStart = gameDayPeriod.PeriodStart;
        conversation = current.PeriodStartedAtUtc == periodStart
            ? current
            : current with { RepliesUsed = 0, PeriodStartedAtUtc = periodStart };
        if (conversation.RepliesUsed >= balance.Value.Relations.DirectMessageRepliesPerDay)
        {
            throw new RequestConflictException("The daily direct-message limit has been reached.");
        }

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
        await starConversationRepository.SaveStarConversationAsync(updated, cancellationToken);
        return new SentDirectMessage(
            generated.Text,
            balance.Value.Relations.DirectMessageRepliesPerDay - updated.RepliesUsed);
    }
}
