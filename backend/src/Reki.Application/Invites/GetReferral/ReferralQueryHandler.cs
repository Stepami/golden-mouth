using Mediator;
using Reki.Application.Invites.Responses;

namespace Reki.Application.Invites.GetReferral;

internal sealed class ReferralQueryHandler :
    IQueryHandler<ReferralQuery, ReferralResponse>
{
    public ValueTask<ReferralResponse> Handle(
        ReferralQuery query,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(new ReferralResponse($"ref_{query.PlayerId.Value:N}"));
}