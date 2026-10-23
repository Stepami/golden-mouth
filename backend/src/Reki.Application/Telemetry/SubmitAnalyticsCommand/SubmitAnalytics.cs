using Mediator;

namespace Reki.Application.Telemetry.SubmitAnalyticsCommand;

public sealed record SubmitAnalytics(AnalyticsSubmission Submission) : ICommand;

