namespace Reki.Application.Posts.GetPublicStoryCard;

public interface IStoryCardRenderer
{
    ValueTask<Stream> RenderAsync(
        StoryCardData card,
        CancellationToken cancellationToken);
}
