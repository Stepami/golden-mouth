namespace Reki.Application.Player.Responses;

public sealed record PlayerStreakResponse(Guid Id, DateTimeOffset StartedAt, int Count, bool IsActive);
