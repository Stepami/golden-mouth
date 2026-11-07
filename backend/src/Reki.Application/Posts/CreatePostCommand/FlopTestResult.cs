namespace Reki.Application.Posts.CreatePostCommand;

public sealed record FlopTestResult(bool IsFlop, string Reason)
{
    public static readonly FlopTestResult Ok = new(false, string.Empty);

    public static FlopTestResult Flop(string reason) => new(true, reason);
}
