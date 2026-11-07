using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand;

public interface IFlopTest
{
    Task<bool> IsFlopAsync(GamePost post);
}
