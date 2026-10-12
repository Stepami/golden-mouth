using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Domain.Events;

public sealed record DailyGameEvent(
    EventId Id,
    PlayerId PlayerId,
    WorldId WorldId,
    string Kind,
    string Badge,
    string Title,
    string Text,
    IReadOnlyList<string> Ideas,
    string Reward,
    string Note,
    DateTimeOffset DayStartedAtUtc,
    string? Answer);