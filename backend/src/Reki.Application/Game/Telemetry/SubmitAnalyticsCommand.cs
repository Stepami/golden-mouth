using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record SubmitAnalyticsCommand(long TelegramUserId, AnalyticsBatch Batch) : IRequest;

