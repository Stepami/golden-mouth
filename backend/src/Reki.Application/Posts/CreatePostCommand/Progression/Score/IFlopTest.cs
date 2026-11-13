using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score;

public interface IFlopTest
{
    int Priority { get; }

    Task<FlopTestResult> IsFlopAsync(GamePost post);
}

public sealed record FlopTestResult(bool IsFlop, string Reason)
{
    public static readonly FlopTestResult Ok = new(false, string.Empty);

    public static FlopTestResult Flop(string reason) => new(true, reason);
}
