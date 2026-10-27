using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI;

public sealed record StarFeedHistoryPost(
    StarId? StarId,
    string Author,
    bool IsOwn,
    string Text);
