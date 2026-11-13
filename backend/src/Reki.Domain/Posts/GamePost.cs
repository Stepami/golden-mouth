using Reki.Domain.Worlds;

namespace Reki.Domain.Posts;

public sealed record GamePost(
    PostId Id,
    AuthorId AuthorId,
    WorldId WorldId,
    string Text,
    long Views,
    long Likes,
    IReadOnlyList<AuthorId> LikedBy,
    IReadOnlyList<GameComment> Comments,
    DateTimeOffset CreatedAtUtc,
    long SubscriberDelta,
    double Quality,
    double ReachRatio,
    bool Viral,
    bool Flop,
    int Version = 0,
    AuthorId? PredictedAuthorId = null,
    string? FlopReason = null,
    PostId? RepostId = null)
{
    public GameComment? FindRequest(string requestId) =>
        Comments.FirstOrDefault(item => string.Equals(item.RequestId, requestId, StringComparison.Ordinal));

    public IReadOnlyList<GameComment> GetBranch(CommentId commentId)
    {
        var branch = new List<GameComment>();
        var visited = new HashSet<CommentId>();
        CommentId? current = commentId;
        while (current is { } id)
        {
            if (!visited.Add(id))
            {
                throw new InvalidOperationException("The comment branch contains a cycle.");
            }

            var comment = Comments.FirstOrDefault(item => item.Id == id)
                          ?? throw new InvalidOperationException("The comment parent is missing.");
            branch.Add(comment);
            current = comment.ParentCommentId;
        }

        branch.Reverse();
        return branch;
    }

    public GamePost AddComments(IReadOnlyList<GameComment> comments)
    {
        var ids = Comments.Select(item => item.Id).ToHashSet();
        var requests = Comments.Select(item => item.RequestId).OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        foreach (var comment in comments)
        {
            if (!ids.Add(comment.Id) ||
                (comment.ParentCommentId is { } parent && (parent == comment.Id || !ids.Contains(parent))) ||
                (comment.RequestId is { } request && !requests.Add(request)))
            {
                throw new InvalidOperationException("The comment identity or parent is invalid.");
            }
        }

        return this with { Comments = [.. Comments, .. comments], Version = Version + 1 };
    }

    public GamePost ApplyProgress(PostProgression progression) =>
        this with
        {
            SubscriberDelta = progression.SubscriberDelta,
            Quality = progression.Quality,
            Viral = progression.Viral,
            Flop = progression.Flop,
            Views = progression.Views,
            Likes = progression.Likes,
            PredictedAuthorId = progression.Commenter.ToAuthorId(),
            FlopReason = progression.Score.FlopReason,
        };
}
