namespace Reki.Domain.Posts;

/// <summary>Тип автора игрового контента.</summary>
public enum AuthorKind
{
    /// <summary>Игрок, которому принадлежит мир.</summary>
    Player,

    /// <summary>AI-звезда из каталога мира.</summary>
    Star,
}