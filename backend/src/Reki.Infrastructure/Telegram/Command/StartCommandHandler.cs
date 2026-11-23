using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application;
using Reki.Application.Player;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Reki.Infrastructure.Telegram.Command;

internal sealed class StartCommandHandler(
    ITelegramBotClient telegram,
    IPlayerRepository players,
    IUnitOfWork unitOfWork,
    IOptions<TelegramOptions> telegramOptions) : IBotCommandHandler, IBotCommand
{
    public static string Command { get; } = BotCommands.Start.Command;

    public async Task Handle(Message message)
    {
        await telegram.SendMessage(
            message.Chat.Id,
            "Попади в РЕКИ - преодолей страх сцены и решись высказаться в игре симуляторе соцсети!",
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(
                "Запустить MiniApp",
                telegramOptions.Value.MiniAppUrl)));

        await unitOfWork.ExecuteAsync(
            async token =>
            {
                var player = await players.GetPlayerAsync(ExternalUserId.Telegram(message.Chat.Id), token);
                if (player is null)
                {
                    return Unit.Value;
                }

                var channel = new TelegramNotificationChannel(message.Chat.Id);
                player.AddNotificationChannel(channel);
                await players.ReplacePlayerAsync(player, token);

                return Unit.Value;
            },
            CancellationToken.None);
    }
}
