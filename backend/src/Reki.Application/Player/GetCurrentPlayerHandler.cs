using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Player;

namespace Reki.Application.Player;

public sealed class GetCurrentPlayerHandler(IRekiContextAccessor contextAccessor)
    : IRequestHandler<GetCurrentPlayerQuery, PlayerResponse>
{
    public async ValueTask<PlayerResponse> Handle(GetCurrentPlayerQuery request, CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        return PlayerResponseMapper.ToResponse(player);
    }
}
