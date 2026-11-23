using Telegram.Bot.Types;

namespace Reki.Infrastructure.Telegram.Command;

public static class BotCommands
{
    public const string StoryInlinePrefix = "story_";

    internal static readonly BotCommand Start = new("start", "Запуск бота");
    internal static readonly BotCommand Help = new("help", "Справка");
    internal static readonly BotCommand Invite = new("invite", "Получить реферальную ссылку");
    internal static readonly BotCommand Stories = new("stories", "Получить фото-карточку для сторис");

    public static readonly IReadOnlyList<BotCommand> All =
    [
        Start,
        Help,
        Invite,
        Stories,
    ];
}
