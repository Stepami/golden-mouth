namespace Reki.Application.Common.Safety;

public static class ContentSafetyPolicy
{
    private static readonly string[] CrisisPhrases =
    [
        "не хочу жить",
        "хочу умереть",
        "убить себя",
        "покончить с собой",
        "самоубий",
        "suicide",
        "kill myself",
    ];

    private static readonly string[] AgeRestrictedPhrases =
    [
        "порно",
        "секс",
        "наркот",
        "героин",
        "кокаин",
        "расчлен",
        "изнасил",
    ];

    private static readonly string[] PersonalHarassmentPhrases =
    [
        "жирн",
        "урод",
        "страшилищ",
        "тупая семья",
        "тупой отец",
        "тупая мать",
    ];

    public static SafetyDecision CheckInput(string text)
    {
        if (ContainsAny(text, CrisisPhrases))
        {
            return SafetyDecision.Crisis;
        }

        return ContainsAny(text, AgeRestrictedPhrases)
            ? SafetyDecision.Rejected
            : SafetyDecision.Allowed;
    }

    public static bool IsOutputAllowed(string text)
    {
        return !ContainsAny(text, CrisisPhrases) &&
            !ContainsAny(text, AgeRestrictedPhrases) &&
            !ContainsAny(text, PersonalHarassmentPhrases);
    }

    private static bool ContainsAny(string text, IEnumerable<string> phrases)
    {
        return phrases.Any(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }
}
