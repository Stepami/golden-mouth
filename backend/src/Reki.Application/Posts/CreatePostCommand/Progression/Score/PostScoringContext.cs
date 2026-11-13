namespace Reki.Application.Posts.CreatePostCommand.Progression.Score;

internal sealed class PostScoringContext
{
    private readonly AsyncLocal<PostScoringData?> _current = new();

    public PostScoringData Current { get => _current.Value ?? throw new InvalidOperationException("Post scoring context is missing."); set => _current.Value = value; }
}
