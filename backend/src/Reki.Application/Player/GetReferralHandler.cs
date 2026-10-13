using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Player;

namespace Reki.Application.Player;

public sealed class GetReferralHandler(IRekiContextAccessor rekiContextAccessor) :
    IRequestHandler<GetReferralQuery, ReferralResponse>
{
    public async ValueTask<ReferralResponse> Handle(
        GetReferralQuery request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        return new ReferralResponse($"ref_{player.Id.Value}");
    }
}
