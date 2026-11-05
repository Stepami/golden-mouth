namespace Reki.Application.Posts.PublishPostCommand;

public enum PostCommitResult
{
    Committed,
    StateChanged,
    InviterChanged,
}
