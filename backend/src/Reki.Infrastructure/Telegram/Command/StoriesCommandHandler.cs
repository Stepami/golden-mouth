using Mediator;
using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Posts.GetPublicStoryCard;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Reki.Infrastructure.Telegram.Command;

internal sealed class StoriesCommandHandler(
    IPlayerRepository players,
    IWorldPlayerCache worldPlayerCache,
    IPostRepository posts,
    ISender sender,
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
        await using var card = await sender.Send(new PublicStoryCardQuery(player.Id, postId));
        await telegram.SendPhoto(
            message.Chat.Id,
            InputFile.FromStream(card, $"{player.Id}_{postId}.png"));
    }
}
