using Mediator;
using Reki.Domain.Telemetry;

namespace Reki.Application.Telemetry;

public sealed record SubmitAnalyticsCommand(long TelegramUserId, AnalyticsBatch Batch) : IRequest;

