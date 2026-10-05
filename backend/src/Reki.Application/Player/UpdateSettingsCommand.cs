using Mediator;

namespace Reki.Application.Player;

public sealed record UpdateSettingsCommand(
    long TelegramUserId,
    string? Drama,
    string? Notifications,
    string? Quiet,
    string? Voice,
    bool? Plus) : IRequest;

