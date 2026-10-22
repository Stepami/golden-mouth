using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Jobs;

public sealed record StarFeedHistoryPost(
    StarId? StarId,
    string Author,
    bool IsOwn,
    string Text);
