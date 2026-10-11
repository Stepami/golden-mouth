using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class GetDirectMessagesHandler(
    IGameStore store,
    IOptions<GameBalance> balance,
    GameDay gameDay,
    ISender sender) : IRequestHandler<GetDirectMessagesQuery, DirectMessageView>
{
    public async ValueTask<DirectMessageView> Handle(
        GetDirectMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var world = await sender.Send(new Reki.Application.Worlds.GetWorldQuery(player.Id, request.WorldId), cancellationToken);
        _ = world.FindStar(request.StarId) ?? throw new ResourceNotFoundException("The star does not belong to this world.");
        var periodStart = gameDay.PeriodStart;
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
            await store.SaveStarConversationAsync(conversation, cancellationToken);
        }

        var repliesLeft = Math.Max(
            0,
            balance.Value.Relations.DirectMessageRepliesPerDay - conversation.RepliesUsed);
        var relation = world.Stars
            .FirstOrDefault(item => item.Id == request.StarId)?.Relation ?? 0;
        return new DirectMessageView(conversation, repliesLeft, relation, world.FindStar(request.StarId) ?? throw new ResourceNotFoundException("The star does not belong to this world."));
    }
}