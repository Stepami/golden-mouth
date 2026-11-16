using Reki.Domain.Player.Streaks;

namespace Reki.Application.Player.Responses;

public sealed record PlayerStreakResponse(StreakId Id, DateTimeOffset StartedAt, int Count, bool IsActive);
