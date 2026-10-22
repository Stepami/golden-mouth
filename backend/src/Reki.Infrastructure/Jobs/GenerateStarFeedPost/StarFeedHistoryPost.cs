using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Jobs.GenerateStarFeedPost;

public sealed record StarFeedHistoryPost(
    StarId? StarId,
    string Author,
    bool IsOwn,
    string Text);
