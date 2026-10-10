using Mediator;

namespace Reki.Application.Telemetry;

public sealed record SubmitAnalyticsCommand(AnalyticsSubmission Submission) : IRequest;

