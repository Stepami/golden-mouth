namespace Reki.Infrastructure.Jobs;

public sealed class JobsOptions
{
    public const string SectionName = "Jobs";

    public bool ExecutionEnabled { get; set; } = true;
}
