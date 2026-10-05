namespace Reki.Domain.Game;

public sealed record PlayerSettings(
    string Drama,
    string Notifications,
    string Quiet,
    string Voice,
    bool Plus);

