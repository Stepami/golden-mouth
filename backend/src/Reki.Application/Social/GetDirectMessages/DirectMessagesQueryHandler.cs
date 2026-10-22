using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Balance;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Social;

namespace Reki.Application.Social.GetDirectMessages;

public sealed class DirectMessagesQueryHandler(
    IStarConversationRepository starConversationRepository,
    IOptions<GameBalance> balance,
    GameDayPeriod gameDayPeriod,
    IRekiContextAccessor rekiContextAccessor) : IQueryHandler<DirectMessagesQuery, DirectMessageView>
{
    public async ValueTask<DirectMessageView> Handle(
        DirectMessagesQuery request,
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
            await starConversationRepository.SaveStarConversationAsync(conversation, cancellationToken);
        }

        var repliesLeft = Math.Max(
            0,
            balance.Value.Relations.DirectMessageRepliesPerDay - conversation.RepliesUsed);
        var relation = world.Stars
            .FirstOrDefault(item => item.Id == request.StarId)?.Relation ?? 0;
        return new DirectMessageView(conversation, repliesLeft, relation, world.FindStar(request.StarId) ?? throw new ResourceNotFoundException("The star does not belong to this world."));
    }
}
