using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reki.Application.Metrics.GetDailyMetrics;

namespace Reki.Infrastructure.Metrics;

internal static class DependencyInjection
{
    public static IServiceCollection AddDailyMetrics(this IServiceCollection services)
    {
        services.AddOptions<DailyMetricsOptions>()
            .BindConfiguration(DailyMetricsOptions.SectionName)
            .Validate(options => options.InitialTotalUsers is null or >= 0, "Initial total users must be nonnegative.")
            .Validate(
                options => options.BaseUrl.Length == 0 ||
                (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"),
                "Daily metrics source must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();
        services.AddSingleton<IDailyMetricsSource, OpenObserveDailyMetricsSource>();
        services.AddHttpApi<IOpenObserveMetricsApi>((apiOptions, provider) =>
            {
                var jsonOptions = provider.GetRequiredService<IOptions<JsonSerializerOptions>>().Value;
                apiOptions.JsonSerializeOptions.TypeInfoResolver = jsonOptions.TypeInfoResolver;
                apiOptions.JsonDeserializeOptions.TypeInfoResolver = jsonOptions.TypeInfoResolver;
                apiOptions.KeyValueSerializeOptions.GetJsonSerializerOptions().TypeInfoResolver = jsonOptions.TypeInfoResolver;
            })
            .ConfigureHttpClient((provider, client) =>
            {
                var source = provider.GetRequiredService<IOptionsMonitor<DailyMetricsOptions>>().CurrentValue;
                if (source.BaseUrl.Length > 0)
                {
                    client.BaseAddress = new Uri(source.BaseUrl);
                }

                client.Timeout = TimeSpan.FromSeconds(8);
            });
        return services;
    }
}
