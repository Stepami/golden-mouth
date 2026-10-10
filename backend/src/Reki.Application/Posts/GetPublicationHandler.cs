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
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        Publication? publication = await store.GetPublicationAsync(request.PublicationId, cancellationToken);
        return publication is not null && publication.PlayerId == player.Id
            ? PostContractMapper.MapPublication(publication)
            : throw new ResourceNotFoundException("Publication not found.");
    }
}
