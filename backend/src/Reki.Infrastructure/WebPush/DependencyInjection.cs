using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;

namespace Reki.Infrastructure.WebPush;

internal static class DependencyInjection
{
    public static IServiceCollection AddWebPush(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<VapidKeyOptions>().BindConfiguration(VapidKeyOptions.SectionName);
        services.AddHttpClient<RekiPushServiceClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            });
        services.AddKeyedSingleton<INotificationSender, WebPushNotificationSender>(NotificationChannelType.WebPush);
        return services;
    }
}