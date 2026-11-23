using System.Security.Cryptography;
using System.Text;
using AspNetCore.Authentication.ApiKey;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Infrastructure.Auth;
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
        services.AddAuthentication().AddApiKeyInHeader(AuthConstants.Schemes.TelegramWebhook, _ => { });
        services.AddOptions<ApiKeyOptions>(AuthConstants.Schemes.TelegramWebhook)
            .Configure<IOptions<TelegramOptions>>((options, telegram) =>
            {
                options.KeyName = "X-Telegram-Bot-Api-Secret-Token";
                options.SuppressWWWAuthenticateHeader = true;
                options.Events.OnValidateKey = context =>
                {
                    var secret = telegram.Value.WebhookSecret;
                    if (!string.IsNullOrEmpty(secret) && CryptographicOperations.FixedTimeEquals(
                            Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(context.ApiKey)))
                    {
                        context.ValidationSucceeded();
                    }
                    else
                    {
                        context.ValidationFailed("Invalid webhook secret.");
                    }

                    return Task.CompletedTask;
                };
            });
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

        services.AddKeyedSingleton<INotificationSender, TelegramNotificationSender>(NotificationChannelType.Telegram);

        return services.AddTelegramAuth(configuration);
    }
}
