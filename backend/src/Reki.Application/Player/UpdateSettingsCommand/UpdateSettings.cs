using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.UpdateSettingsCommand;

public sealed record UpdateSettings(
    string? Notifications,
    string? Quiet,
    bool? Plus,
    Address? Address) : ICommand;
