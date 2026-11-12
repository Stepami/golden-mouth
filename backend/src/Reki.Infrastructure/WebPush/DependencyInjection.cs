using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.Notifications;

namespace Reki.Infrastructure.WebPush;

internal static class DependencyInjection
{
    public static IServiceCollection AddWebPush(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<VapidKeyOptions>().BindConfiguration(VapidKeyOptions.SectionName);
        services.AddHttpClient<RekiPushServiceClient>()
            .ConfigurePrimaryHttpMessageHandler(
                () => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddSingleton<INotificationSender, WebPushNotificationSender>();
        return services;
    }
}