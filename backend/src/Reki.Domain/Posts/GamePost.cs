using Reki.Domain.Worlds;

namespace Reki.Domain.Posts;

public sealed class GamePost
{
    private readonly List<AuthorId> _likedBy;
    private readonly List<GameComment> _comments;

    public GamePost(
        PostId id,
        AuthorId authorId,
        WorldId worldId,
        string text,
        long views,
        long likes,
        IReadOnlyList<AuthorId> likedBy,
        IReadOnlyList<GameComment> comments,
        DateTimeOffset createdAtUtc,
        long subscriberDelta,
        double quality,
        double reachRatio,
        bool viral,
        bool flop,
        int version = 0,
        AuthorId? predictedAuthorId = null,
        string? flopReason = null,
        PostId? repostId = null)
    {
        Id = id;
        AuthorId = authorId;
        WorldId = worldId;
        Text = text;
        Views = views;
        Likes = likes;
        _likedBy = [.. likedBy];
        LikedBy = _likedBy.AsReadOnly();
        _comments = [.. comments];
        Comments = _comments.AsReadOnly();
        CreatedAtUtc = createdAtUtc;
        SubscriberDelta = subscriberDelta;
        Quality = quality;
        ReachRatio = reachRatio;
        Viral = viral;
        Flop = flop;
        Version = version;
        PredictedAuthorId = predictedAuthorId;
        FlopReason = flopReason;
        RepostId = repostId;
    }

    public PostId Id { get; private set; }

    public AuthorId AuthorId { get; private set; }

    public WorldId WorldId { get; private set; }

    public string Text { get; private set; }

    public long Views { get; private set; }

    public long Likes { get; private set; }

    public IReadOnlyList<AuthorId> LikedBy { get; }

    public IReadOnlyList<GameComment> Comments { get; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public long SubscriberDelta { get; private set; }

    public double Quality { get; private set; }

    public double ReachRatio { get; private set; }

    public bool Viral { get; private set; }

    public bool Flop { get; private set; }

    public int Version { get; private set; }

    public AuthorId? PredictedAuthorId { get; private set; }

    public string? FlopReason { get; private set; }

    public PostId? RepostId { get; private set; }

    public bool IsLikedBy(AuthorId authorId) => _likedBy.Contains(authorId);

    public bool ToggleLike(AuthorId authorId)
    {
        var liked = !_likedBy.Remove(authorId);
        if (liked)
        {
            _likedBy.Add(authorId);
        }

        Version++;
        return liked;
    }

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

    public void AddComments(IReadOnlyList<GameComment> comments)
    {
        if (comments.Count == 0)
        {
            return;
        }

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

        _comments.AddRange(comments);
        Version++;
    }

    public void ApplyProgress(PostProgression progression)
    {
        var predictedAuthorId = progression.Commenter.ToAuthorId();
        if (SubscriberDelta == progression.SubscriberDelta && Quality.CompareTo(progression.Quality) == 0 &&
            Viral == progression.Viral && Flop == progression.Flop && Views == progression.Views &&
            Likes == progression.Likes && PredictedAuthorId == predictedAuthorId && FlopReason == progression.Score.FlopReason)
        {
            return;
        }

        SubscriberDelta = progression.SubscriberDelta;
        Quality = progression.Quality;
        Viral = progression.Viral;
        Flop = progression.Flop;
        Views = progression.Views;
        Likes = progression.Likes;
        PredictedAuthorId = predictedAuthorId;
        FlopReason = progression.Score.FlopReason;
        Version++;
    }
}
