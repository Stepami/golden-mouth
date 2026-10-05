namespace Reki.Application.Game.Common;

public interface IStoryCardRenderer
{
    ValueTask RenderAsync(
        string postId,
        Stream target,
        CancellationToken cancellationToken);
}
