using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Domain.Posts;

/// <summary>Преобразования идентификаторов игрока и звезды в идентификатор автора.</summary>
public static class AuthorIdExtensions
{
    public static AuthorId ToAuthor(this PlayerId id) => AuthorId.From((AuthorKind.Player, id.Value));

    public static AuthorId ToAuthor(this StarId id) => AuthorId.From((AuthorKind.Star, id.Value));
}