namespace Reki.Infrastructure.Jobs;

internal sealed class JobsOptions
{
    public const string SectionName = "Jobs";

    public bool ExecutionEnabled { get; set; } = true;

    public int AuthPruneAfterDays { get; set; } = 30;
}
