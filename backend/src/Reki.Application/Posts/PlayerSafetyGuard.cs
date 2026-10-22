using Reki.Application.Common.Exceptions;
using Reki.Application.Player;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed class PlayerSafetyGuard(
    IPlayerRepository playerRepository,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan HateCooldown = TimeSpan.FromDays(7);
    private static readonly string[] CrisisPhrases =
    [
        "не хочу жить", "хочу умереть", "убить себя", "покончить с собой", "самоубий", "suicide", "kill myself",
    ];

    private static readonly string[] AgeRestrictedPhrases =
    [
        "порно", "секс", "наркот", "героин", "кокаин", "расчлен", "изнасил",
    ];

    private static readonly string[] PersonalHarassmentPhrases =
    [
        "жирн", "урод", "страшилищ", "тупая семья", "тупой отец", "тупая мать",
    ];

    public static bool IsOutputAllowed(string text) => OutputRejectionCategory(text) is null;

    public static void EnsureOutputAllowed(IEnumerable<string> texts)
    {
        foreach (var text in texts)
        {
            if (OutputRejectionCategory(text) is { } category)
            {
                throw new UnsafeGeneratedContentException(category);
            }
        }
    }

    public async ValueTask EnsureInputAllowedAsync(
        PlayerProfile player,
        IEnumerable<string> texts,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var decision = SafetyDecision.Allowed;
        foreach (var text in texts)
        {
            if (ContainsAny(text, CrisisPhrases))
            {
                decision = SafetyDecision.Crisis;
                break;
            }

            if (ContainsAny(text, AgeRestrictedPhrases))
            {
                decision = SafetyDecision.Rejected;
            }
        }

        if (decision == SafetyDecision.Allowed)
        {
            return;
        }

        if (decision == SafetyDecision.Rejected)
        {
            throw new ContentRejectedException("explicit_content");
        }

        var now = timeProvider.GetUtcNow();
        var paused = player.StartCrisisPause(now, HateCooldown);
        await playerRepository.TryReplacePlayerAsync(paused, player.Version, cancellationToken);

        throw new CrisisDetectedException(player.BirthGroup == "14-17");
    }

    private static string? OutputRejectionCategory(string text)
    {
        if (ContainsAny(text, CrisisPhrases))
        {
            return "crisis";
        }

        if (ContainsAny(text, AgeRestrictedPhrases))
        {
            return "explicit_content";
        }

        return ContainsAny(text, PersonalHarassmentPhrases) ? "harassment" : null;
    }

    private static bool ContainsAny(string text, IEnumerable<string> phrases) =>
        phrases.Any(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase));
}
