using Reki.Application.Posts;

namespace Reki.Application.Common;

public interface IStoryCardRenderer
{
    ValueTask RenderAsync(
        StoryCardData card,
        Stream target,
        CancellationToken cancellationToken);
}
