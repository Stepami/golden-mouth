using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed record GetPublicationQuery(
    TelegramUserId TelegramUserId,
    string PublicationId) : IQuery<PublicationStatusResponse>;
