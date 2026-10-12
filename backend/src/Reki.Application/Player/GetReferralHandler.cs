using Mediator;
using Reki.Contracts.Player;

namespace Reki.Application.Player;

public sealed class GetReferralHandler(
    ISender sender) : IRequestHandler<GetReferralQuery, ReferralResponse>
{
    public async ValueTask<ReferralResponse> Handle(GetReferralQuery request, CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        return new ReferralResponse($"ref_{player.Id.Value}");
    }
}
