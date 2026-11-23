using Mediator;
using Reki.Application.Invites.GetReferral;
using Reki.Application.Player;
using Reki.Domain.Player;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Reki.Infrastructure.Telegram.Command;

internal sealed class InviteCommandHandler(
    IPlayerRepository players,
    ISender sender,
    ITelegramBotClient telegram) : IBotCommandHandler, IBotCommand
{
    public static string Command { get; } = BotCommands.Invite.Command;

    public async Task Handle(Message message)
    {
        var player = await players.GetPlayerAsync(ExternalUserId.Telegram(message.Chat.Id), default);
        if (player is null)
        {
            await telegram.SendMessage(message.Chat.Id, "Игрок с указанным Telegram Id не найден");
            return;
        }

        var referral = await sender.Send(new ReferralQuery(player.Id));
        var link = $"https://reki-app.ru/?ref={referral.Code}";
        await telegram.SendMessage(
            message.Chat.Id,
            link,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(
                "Отправить",
                $"https://t.me/share/url?url={Uri.EscapeDataString(link)}")));
    }
}
