using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Feed;

public sealed record StarFeedHistoryPost(
    StarId? StarId,
    string Author,
    bool IsOwn,
    string Text);
