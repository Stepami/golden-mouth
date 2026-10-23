namespace Reki.Application.Telemetry;

public enum LaunchSurface
{
    /// <summary>Способ запуска неизвестен.</summary>
    Unknown,

    /// <summary>Мини-приложение Telegram.</summary>
    TelegramMiniApp,

    /// <summary>Запуск в браузере.</summary>
    Browser,

    /// <summary>Установленное веб-приложение.</summary>
    Standalone,
}
