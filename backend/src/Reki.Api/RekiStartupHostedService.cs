using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Options;
using Reki.Infrastructure.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Reki.Api;

internal sealed class RekiStartupHostedService(
    IMigrator migrator,
    IOptions<TelegramOptions> telegramOptions,
    ITelegramBotClient telegramBotClient) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await migrator.MigrateAsync(cancellationToken: cancellationToken);

        await telegramBotClient.SetWebhook(
            telegramOptions.Value.WebhookUrl,
            allowedUpdates: [UpdateType.Message],
            dropPendingUpdates: false,
            secretToken: telegramOptions.Value.WebhookSecret,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
