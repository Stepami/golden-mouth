using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Player;

namespace Reki.Application.Player;

public sealed class GetReferralHandler(
    IRekiContextAccessor contextAccessor) : IRequestHandler<GetReferralQuery, ReferralResponse>
{
    public async ValueTask<ReferralResponse> Handle(GetReferralQuery request, CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        return new ReferralResponse($"ref_{player.Id.Value}");
    }
}
