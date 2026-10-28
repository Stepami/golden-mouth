using Vogen;

namespace Reki.Domain.Worlds;

[ValueObject<StarFeedEventType>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct StarFeedEvent
{
    public string GetInstruction() => Value switch
    {
        StarFeedEventType.FeedOpened => "Напиши один самостоятельный пост: добавь новое событие, наблюдение или действие в рамках личности и мира. Выбери другой повод, предмет или место; не продолжай ситуацию из последней собственной записи.",
        StarFeedEventType.PostPublished => "Напиши один пост-реакцию на конкретный исходный пост игрока. Ответь на его смысл и добавь собственную позицию.",
        StarFeedEventType.CommentPublished => "Напиши один пост-реакцию на конкретный комментарий игрока с учётом исходного поста и его ветки. Ответь на смысл комментария и добавь собственную позицию.",
        _ => string.Empty,
    };

    // Vogen 8 does not generate this IConvertible member for enum-backed values.
    public string ToString(IFormatProvider? provider) => ToString();

    private static Validation Validate(StarFeedEventType value) =>
        Enum.IsDefined(value) ? Validation.Ok : Validation.Invalid("Unknown feed event.");
}
