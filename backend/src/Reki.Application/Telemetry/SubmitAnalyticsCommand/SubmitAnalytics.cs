using Mediator;

namespace Reki.Application.Telemetry.SubmitAnalyticsCommand;

public sealed record SubmitAnalytics(AnalyticsSubmission Submission, bool Anonymous = false) : ICommand;

