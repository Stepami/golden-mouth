using Mediator;

namespace Reki.Application.Player.UpdateSettingsCommand;

public sealed record UpdateSettings(
    string? Notifications,
    string? Quiet,
    bool? Plus,
    string? Address) : ICommand;