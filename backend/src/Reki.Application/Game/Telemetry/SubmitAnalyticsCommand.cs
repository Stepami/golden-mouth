using Mediator;
using Reki.Domain.Game.Telemetry;

namespace Reki.Application.Game.Telemetry;

public sealed record SubmitAnalyticsCommand(long TelegramUserId, AnalyticsBatch Batch) : IRequest;

