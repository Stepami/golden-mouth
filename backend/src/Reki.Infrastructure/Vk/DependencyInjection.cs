using System.Text.Json;
using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Reki.Application.Auth;
using Reki.Infrastructure.Auth;

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
        services.AddHttpApi<IVkApi>((options, provider) =>
            {
                var jsonOptions = provider.GetRequiredService<IOptions<JsonSerializerOptions>>().Value;
                options.JsonSerializeOptions.TypeInfoResolver = jsonOptions.TypeInfoResolver;
                options.JsonDeserializeOptions.TypeInfoResolver = jsonOptions.TypeInfoResolver;
                options.KeyValueSerializeOptions.GetJsonSerializerOptions().TypeInfoResolver = jsonOptions.TypeInfoResolver;
            })
            .ConfigureHttpClient((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<VkOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
        services.AddAuthentication().AddApiKeyInAuthorizationHeader(AuthConstants.Schemes.Vk, options =>
        {
            options.KeyName = "Bearer";
            options.SuppressWWWAuthenticateHeader = true;

            // This scheme only extracts the credential; the provider verifies it before issuing a session.
            options.Events.OnValidateKey = context =>
            {
                context.Properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = context.ApiKey }]);
                context.ValidationSucceeded();
                return Task.CompletedTask;
            };
        });
        services.AddKeyedScoped<IExternalUserIdProvider, VkUserIdProvider>(AuthConstants.Grants.Vk);
        return services;
    }
}
