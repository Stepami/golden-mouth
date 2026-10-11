using Mediator;

namespace Reki.Application.Player;

public sealed record UpdateSettingsCommand(
    string? Drama,
    string? Notifications,
    string? Quiet,
    string? Voice,
    bool? Plus,
    string? Address) : IRequest;
