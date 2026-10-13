using Reki.Domain.Player;

namespace Reki.Application.Common.Safety;

public interface IContentModerator
{
    ValueTask<ContentReview> ReviewAsync(PlayerProfile player, IReadOnlyList<string> texts, CancellationToken cancellationToken);
}

public sealed record ContentReview(SafetyDecision Decision, string Category);
