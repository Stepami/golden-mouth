using Mediator;
using Reki.Application.Player.Responses;
using Reki.Application.RekiContext;

namespace Reki.Application.Player.GetReferral;

internal sealed class ReferralQueryHandler(IRekiContextAccessor rekiContextAccessor) :
    IQueryHandler<ReferralQuery, ReferralResponse>
{
    public ValueTask<ReferralResponse> Handle(
        ReferralQuery query,
        CancellationToken cancellationToken)
    {
        var playerId = rekiContextAccessor.Context.PlayerId;
        return ValueTask.FromResult(new ReferralResponse($"ref_{playerId.Value:N}"));
    }
}