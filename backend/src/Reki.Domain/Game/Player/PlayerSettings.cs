namespace Reki.Domain.Game.Player;

public sealed record PlayerSettings(
    string Drama,
    string Notifications,
    string Quiet,
    string Voice,
    bool Plus);

