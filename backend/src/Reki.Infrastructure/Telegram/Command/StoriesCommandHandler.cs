using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Reki.Infrastructure.Telegram.Command;

internal sealed class StoriesCommandHandler(
    IPlayerRepository players,
    IWorldPlayerCache worldPlayerCache,
    IPostRepository posts,
    ITelegramBotClient telegram) : IBotCommandHandler, IBotCommand
{
    public static string Command { get; } = BotCommands.Stories.Command;

    public async Task Handle(Message message)
    {
        var ct = CancellationToken.None;
        var player = await players.GetPlayerAsync(ExternalUserId.Telegram(message.Chat.Id), ct);
        if (player is null)
        {
            await telegram.SendMessage(message.Chat.Id, "Игрок с указанным Telegram Id не найден");
            return;
        }

        var worlds = await worldPlayerCache.GetWorldsAsync(player.Id, ct);
        if (worlds.Count is 0)
        {
            await telegram.SendMessage(message.Chat.Id, "У тебя ещё нет миров");
            return;
        }

        var worldId = worlds.Keys.First();
        var playerPosts = await posts.GetPostsAsync(worldId, [player.Id.ToAuthorId()], null, 1, null, CancellationToken.None);
        if (playerPosts is { Items: [] })
        {
            await telegram.SendMessage(message.Chat.Id, "У тебя ещё нет постов");
            return;
        }

        var postId = playerPosts.Items[0].Id;
        var cardUrl = $"https://reki-app.ru/api/v1/cards/story/{player.Id.Value:N}/{postId.Value:N}.png";
        await telegram.SendMessage(
            message.Chat.Id,
            "Вот твоя карточка - сохрани её для сторис!",
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl("Открыть карточку", cardUrl)));
    }
}
