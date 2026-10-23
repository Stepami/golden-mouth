namespace Reki.Application.Posts.GetPublicStoryCard;

public interface IStoryCardRenderer
{
    ValueTask RenderAsync(
        StoryCardData card,
        Stream target,
        CancellationToken cancellationToken);
}
