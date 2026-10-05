using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Telemetry;

public sealed record SubmitAnalyticsCommand(
    TelegramUserId TelegramUserId,
    AnalyticsSubmission Submission) : IRequest;

