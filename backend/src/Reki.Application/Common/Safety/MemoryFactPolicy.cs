namespace Reki.Application.Common.Safety;

public static class MemoryFactPolicy
{
    private static readonly string[] InstructionMarkers =
    [
        "ignore", "override", "system prompt", "developer", "star_id", "tool", "instruction",
        "игнорир", "системный промпт", "системные инструкции", "смени роль", "сменить роль",
        "начисли", "подписчик", "лимит", "обойди", "безопасност", "следуй команде", "выполни",
        "[system]", "[assistant]", "<system", "DATA_JSON", "ДОВЕРЕННЫЙ КАТАЛОГ",
    ];

    public static bool IsAllowed(string fact)
    {
        return fact.Length is > 0 and <= 160 &&
            !fact.Any(char.IsControl) &&
            PlayerSafetyGuard.IsOutputAllowed(fact) &&
            !InstructionMarkers.Any(marker => fact.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
