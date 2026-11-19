namespace Reki.Application.Telemetry;

public sealed record ProductGoalResult<T>(T Result, IReadOnlyList<string> Goals);
