using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record UpdateSettingsCommand(
    long TelegramUserId,
    string? Drama,
    string? Notifications,
    string? Quiet,
    string? Voice,
    bool? Plus) : IRequest;

