using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record UpdateSettingsCommand(
    TelegramUserId TelegramUserId,
    string? Drama,
    string? Notifications,
    string? Quiet,
    string? Voice,
    bool? Plus,
    string? Address) : IRequest;
