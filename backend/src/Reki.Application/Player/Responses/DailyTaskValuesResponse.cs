using Reki.Domain.Worlds;

namespace Reki.Application.Player.Responses;

public sealed record DailyTaskValuesResponse(long Target, long Progress, StarId? StarId);
