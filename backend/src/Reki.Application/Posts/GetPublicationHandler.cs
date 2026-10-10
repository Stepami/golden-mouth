using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Posts;

public sealed class GetPublicationHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    PlayerReader playerReader) : IQueryHandler<GetPublicationQuery, PublicationStatusResponse>
{
    public async ValueTask<PublicationStatusResponse> Handle(
        GetPublicationQuery request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        var publication = await store.GetPublicationAsync(request.PublicationId, cancellationToken);
        return publication is not null && publication.PlayerId == player.Id
            ? PostContractMapper.MapPublication(publication)
            : throw new ResourceNotFoundException("Publication not found.");
    }
}
