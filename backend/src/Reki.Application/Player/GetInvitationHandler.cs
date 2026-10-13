using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Contracts.Player;

namespace Reki.Application.Player;

public sealed class GetInvitationHandler(
    IRekiContextAccessor contextAccessor,
    IGameStore store,
    IOptions<GameBalance> balance) : IRequestHandler<GetInvitationQuery, InvitationResponse>
{
    public async ValueTask<InvitationResponse> Handle(GetInvitationQuery request, CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        var invitation = await store.GetPendingInviteAsync(player.Id, cancellationToken);
        return new InvitationResponse(invitation is null ? 0 : balance.Value.InviteBonusBoth);
    }
}
