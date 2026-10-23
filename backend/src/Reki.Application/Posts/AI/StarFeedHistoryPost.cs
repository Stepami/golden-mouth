using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI;

internal sealed record StarFeedHistoryPost(
    StarId? StarId,
    string Author,
    bool IsOwn,
    string Text);
