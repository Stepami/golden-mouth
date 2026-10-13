using Reki.Domain.Social;

namespace Reki.Infrastructure.Jobs;

public sealed record StarFeedHistoryPost(StarId? StarId, string Author, bool IsOwn, string Text);
