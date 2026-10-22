using Mediator;
using Reki.Application.RekiContext;
using Reki.Contracts.Player;

namespace Reki.Application.Player.GetReferral;

internal sealed class ReferralQueryHandler(IRekiContextAccessor rekiContextAccessor) :
    IQueryHandler<ReferralQuery, ReferralResponse>
{
    public async ValueTask<ReferralResponse> Handle(
        ReferralQuery request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        return new ReferralResponse($"ref_{player.Id.Value}");
    }
}
