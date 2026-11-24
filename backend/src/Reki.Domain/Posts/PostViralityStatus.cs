using Reki.Domain.Bonuses;

namespace Reki.Domain.Posts;

/// <summary>
/// Статус виральности поста <see cref="GamePost"/>.
/// </summary>
public enum PostViralityStatus
{
    /// <summary>
    /// Обычный пост.
    /// </summary>
    None,

    /// <summary>
    /// Залетел.
    /// </summary>
    Viral,

    /// <summary>
    /// Провалился.
    /// </summary>
    Flop,

    /// <summary>
    /// Применился бонус <see cref="ViralCardBonus"/>.
    /// </summary>
    ViralCard,

    /// <summary>
    /// Пост провалился, но сработал бонус <see cref="FlopGuardBonus"/>.
    /// </summary>
    FlopGuard,
}