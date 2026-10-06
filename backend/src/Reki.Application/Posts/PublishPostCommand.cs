using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed record PublishPostCommand(
    TelegramUserId TelegramUserId,
    string Text,
    bool OwnWords,
    string? WantId,
    string? PublicationId) : IRequest<PublishedPost>
{
    public static PublishPostCommand ForPublication(
        TelegramUserId telegramUserId,
        string publicationId) =>
        new(telegramUserId, string.Empty, true, null, publicationId);
}

