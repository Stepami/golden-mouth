namespace Reki.Infrastructure.Telegram.Command;

public interface IBotCommand
{
    static abstract string Command { get; }
}