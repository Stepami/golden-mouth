using Reki.Domain.Worlds;

namespace Reki.Domain.Posts;

public sealed class GameComment
{
    private readonly List<CommentReaction> _reactions;

    public GameComment(
        CommentId id,
        CommentId? parentCommentId,
        string? requestId,
        AuthorId authorId,
        string text,
        bool hidden,
        IReadOnlyList<CommentReaction> reactions,
        StarId? additionalReplierId)
    {
        Id = id;
        ParentCommentId = parentCommentId;
        RequestId = requestId;
        AuthorId = authorId;
        Text = text;
        Hidden = hidden;
        _reactions = [.. reactions];
        AdditionalReplierId = additionalReplierId;
    }

    public CommentId Id { get; private set; }

    public CommentId? ParentCommentId { get; private set; }

    public string? RequestId { get; private set; }

    public AuthorId AuthorId { get; private set; }

    public string Text { get; private set; }

    public bool Hidden { get; private set; }

    public IReadOnlyList<CommentReaction> Reactions => field ??= _reactions.AsReadOnly();

    public StarId? AdditionalReplierId { get; private set; }
}
