using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Reki.Application.Notifications;
using Reki.Infrastructure.Telegram.Auth;
using Telegram.Bot;

namespace Reki.Infrastructure.Telegram;

internal static class DependencyInjection
{
    public static IServiceCollection AddTelegram(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>().BindConfiguration(TelegramOptions.SectionName);
        services.AddTransient<IOptionsFactory<TelegramBotClientOptions>, TelegramBotClientOptionsFactory>();

        services.AddRedaction();
        services.AddDownstreamDependencyMetadata<TelegramBotTelemetry>();
        services.AddOpenTelemetry().WithTracing(tracing => tracing.AddProcessor<TelegramBotTelemetry>());

        services.AddHttpClient<ITelegramBotClient, RekiTelegramBotClient>()
            .AddExtendedHttpClientLogging(options =>
            {
                options.RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.Strict;
                options.RequestPathLoggingMode = OutgoingPathLoggingMode.Formatted;
                options.LogBody = false;
            });

        services.AddSingleton<INotificationSender, TelegramNotificationSender>();

        return services.AddTelegramAuth(configuration);
    }
}