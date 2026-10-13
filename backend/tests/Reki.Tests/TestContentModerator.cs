using Reki.Application.Common.Safety;
using Reki.Domain.Player;

namespace Reki.Tests;

/// <summary>Deterministic safety fixture for component tests without external model calls.</summary>
internal sealed class TestContentModerator : IContentModerator
{
    public ValueTask<ContentReview> ReviewAsync(PlayerProfile player, IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        var decision = texts.Select(ContentSafetyPolicy.CheckInput).OrderByDescending(value => value).FirstOrDefault();
        if (decision == SafetyDecision.Allowed && texts.Any(text => !ContentSafetyPolicy.IsOutputAllowed(text)))
        {
            decision = SafetyDecision.Rejected;
        }

        return ValueTask.FromResult(new ContentReview(decision, decision == SafetyDecision.Crisis ? "crisis" : "explicit_content"));
    }
}
