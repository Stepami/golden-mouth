namespace Reki.Domain.Player.Challenges;

/// <summary>Тип ежедневного задания игрока.</summary>
public enum DailyTaskType
{
    /// <summary>Опубликовать заданное количество собственных постов без репостов и цитат.</summary>
    Posts,

    /// <summary>Сделать заданное количество репостов или цитат.</summary>
    Reposts,

    /// <summary>Написать заданное количество комментариев от имени игрока.</summary>
    Comments,

    /// <summary>Поставить заданное количество лайков на постах.</summary>
    Likes,

    /// <summary>Получить комментарий выбранной звезды под постом игрока.</summary>
    StarComment,

    /// <summary>Набрать заданное количество подписчиков за день за счёт публикаций.</summary>
    Subscribers,

    /// <summary>Получить заданное количество отписок за день после публикаций.</summary>
    Unsubscribers,

    /// <summary>Опубликовать пост, который наберёт заданное количество просмотров.</summary>
    PostViews,

    /// <summary>Опубликовать пост, который при публикации наберёт заданное количество лайков.</summary>
    PostLikes,
}
