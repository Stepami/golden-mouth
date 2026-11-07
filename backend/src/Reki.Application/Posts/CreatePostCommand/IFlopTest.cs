using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand;

public interface IFlopTest
{
    int Priority { get; }

    Task<FlopTestResult> IsFlopAsync(GamePost post);
}
