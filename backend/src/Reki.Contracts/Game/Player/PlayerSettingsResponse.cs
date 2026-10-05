namespace Reki.Contracts.Game.Player;

public sealed record PlayerSettingsResponse(
    Drama Drama,
    string Notifications,
    string Quiet,
    string Voice,
    bool Plus);

