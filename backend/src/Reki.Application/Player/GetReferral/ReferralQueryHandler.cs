using Mediator;
using Reki.Application.Player.GetReferral;
using Reki.Application.Player.Responses;

namespace Reki.Application.Invites.GetReferral;

internal sealed class ReferralQueryHandler :
    IQueryHandler<ReferralQuery, ReferralResponse>
{
    public ValueTask<ReferralResponse> Handle(
        ReferralQuery query,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(new ReferralResponse($"ref_{query.PlayerId.Value:N}"));
}