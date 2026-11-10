using System.Security.Claims;
using System.Text.Json;
using AspNetCore.Authentication.ApiKey;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Reki.Application.Auth;
using Reki.Domain.Player;
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
                options.UseLogging = false;
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
            .RemoveAllLoggers()
            .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
        services.AddAuthentication().AddApiKeyInAuthorizationHeader(AuthConstants.Schemes.Vk, options =>
        {
            options.KeyName = "Bearer";
            options.SuppressWWWAuthenticateHeader = true;
            options.Events.OnValidateKey = async context =>
            {
                var httpContext = context.HttpContext;
                try
                {
                    var provider = httpContext.RequestServices;
                    var vkOptions = provider.GetRequiredService<IOptions<VkOptions>>().Value;
                    var info = await provider.GetRequiredService<IVkApi>()
                        .GetUserInfoAsync(vkOptions.ApplicationId, context.ApiKey, httpContext.RequestAborted);
                    if (info.User?.UserId is { Length: > 0 } id && id.All(char.IsAsciiDigit) && id.Any(character => character != '0'))
                    {
                        context.ValidationSucceeded([new Claim(AuthConstants.Claims.ExternalUserId, ExternalUserId.Vk(id).Value)]);
                    }
                    else
                    {
                        context.ValidationFailed();
                    }
                }
                catch (Exception) when (!httpContext.RequestAborted.IsCancellationRequested)
                {
                    context.ValidationFailed();
                }
            };
        });
        services.AddKeyedScoped<IExternalUserIdProvider, VkUserIdProvider>(AuthConstants.Grants.Vk);
        return services;
    }
}
