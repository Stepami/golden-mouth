using Reki.Domain.Player;

namespace Reki.Domain.Events;

public sealed record DailyGameEvent(
    EventId Id,
    PlayerId PlayerId,
    string Kind,
    string Badge,
    string Title,
    string Text,
    IReadOnlyList<string> Ideas,
    string Reward,
    string Note,
    DateTimeOffset DayStartedAtUtc,
    string? Answer);

