namespace Reki.Contracts.Posts;

/// <summary>Состояние генерации ответа в треде.</summary>
public enum ReplyStatus
{
    /// <summary>Ответ генерируется и ещё не опубликован.</summary>
    Generating,

    /// <summary>Ответ сохранён вместе с энергией и бонусом.</summary>
    Completed,

    /// <summary>Ответ не опубликован из-за ошибки.</summary>
    Failed,
}
