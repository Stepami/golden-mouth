using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Player;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed class GetReferralHandler(PlayerReader playerReader) : IRequestHandler<GetReferralQuery, ReferralResponse>
{
    public async ValueTask<ReferralResponse> Handle(GetReferralQuery request, CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        return new ReferralResponse($"ref_{player.Id.Value}");
    }
}
