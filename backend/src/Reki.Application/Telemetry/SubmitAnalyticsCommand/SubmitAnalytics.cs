using Mediator;

namespace Reki.Application.Telemetry.SubmitAnalyticsCommand;

public sealed record SubmitAnalytics(AnalyticsSubmission Submission, bool Anonymous = false) : ICommand
{
    public static bool IsLandingEvent(string name) => name is
        "landing_view" or "landing_continue" or "landing_auth_complete" or "landing_referral_open" or
        "landing_world_selected" or "landing_character_selected" or "landing_reaction_viewed";

    public bool ContainsProfileLink() => Submission.Events.Any(entry => entry.Name == "landing_profile_link");
}
