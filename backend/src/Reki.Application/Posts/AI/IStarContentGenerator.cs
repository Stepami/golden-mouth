namespace Reki.Application.Posts.AI;

public interface IStarContentGenerator
{
    /// <summary>Returns the generated feed text, or null when it fails content validation.</summary>
    ValueTask<string?> GenerateContentAsync(
        IStarFeedContext context,
        CancellationToken cancellationToken);
}
