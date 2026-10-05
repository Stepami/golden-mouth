namespace Reki.Application.Game;

public interface IStoryCardRenderer
{
    ValueTask RenderAsync(
        string postId,
        Stream target,
        CancellationToken cancellationToken);
}
