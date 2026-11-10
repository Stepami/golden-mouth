using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Reki.Infrastructure.Vk;

internal static class DependencyInjection
{
    public static IServiceCollection AddVk(this IServiceCollection services)
    {
        services.AddOptions<VkOptions>()
            .BindConfiguration(VkOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http",
                "Vk:BaseUrl must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();
        services.AddHttpApi<IVkApi>(options =>
            {
                options.UseLogging = false;
                options.PrependJsonSerializerContext(VkJsonSerializerContext.Default);
            })
            .ConfigureHttpClient((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<VkOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .RemoveAllLoggers()
            .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
        services.AddScoped<VkIdentityVerifier>();
        return services;
    }
}
