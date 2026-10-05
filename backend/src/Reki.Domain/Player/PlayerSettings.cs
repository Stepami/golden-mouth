namespace Reki.Domain.Player;

public sealed record PlayerSettings(
    string Drama,
    string Notifications,
    string Quiet,
    string Voice,
    bool Plus);

