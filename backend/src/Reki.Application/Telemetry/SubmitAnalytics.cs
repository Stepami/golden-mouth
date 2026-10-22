using Mediator;

namespace Reki.Application.Telemetry;

public sealed record SubmitAnalytics(AnalyticsSubmission Submission) : ICommand;

