using Telegram.Bot;
using Telegram.Bot.Types;

namespace Reki.Infrastructure.Telegram.Command;

internal sealed class HelpCommandHandler(ITelegramBotClient telegram) : IBotCommandHandler, IBotCommand
{
    public static string Command { get; } = BotCommands.Help.Command;

    public async Task Handle(Message message) =>
        await telegram.SendMessage(
            message.Chat.Id,
            BotCommands.All
                .Select(command => $"/{command.Command} — {command.Description}")
                .Aggregate(string.Empty, (current, command) => $"{current}\n{command}"));
}
