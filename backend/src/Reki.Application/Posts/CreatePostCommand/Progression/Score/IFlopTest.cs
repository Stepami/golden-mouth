using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score;

public interface IFlopTest
{
    int Priority { get; }

    ValueTask<FlopTestResult> IsFlopAsync(GamePost post, World world);
}

public sealed record FlopTestResult(bool IsFlop, string Reason)
{
    public static readonly FlopTestResult Ok = new(false, string.Empty);

    public static FlopTestResult Flop(string reason) => new(true, reason);
}
