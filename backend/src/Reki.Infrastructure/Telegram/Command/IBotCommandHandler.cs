using Telegram.Bot.Types;

namespace Reki.Infrastructure.Telegram.Command;

public interface IBotCommandHandler
{
    Task Handle(Message message);
}