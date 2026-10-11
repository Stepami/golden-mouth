using Mediator;

namespace Reki.Application.Player;

public sealed record UpdateSettingsCommand(
    string? Notifications,
    string? Quiet,
    bool? Plus,
    string? Address) : IRequest;