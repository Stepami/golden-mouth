using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed record GeneratePublicationCommand(
    string Id,
    TelegramUserId TelegramUserId,
    string Text,
    bool OwnWords,
    string? WantId) : IStreamRequest<PublicationStatusLiveEvent>;
