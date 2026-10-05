namespace Reki.Domain.Game;

public sealed record DailyGameEvent(
    string Id,
    string PlayerId,
    string Kind,
    string Badge,
    string Title,
    string Text,
    IReadOnlyList<string> Ideas,
    string Reward,
    string Note,
    DateTimeOffset DayStartedAtUtc,
    string? Answer);

