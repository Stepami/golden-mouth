using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Reki.Application.Auth;

namespace Reki.Infrastructure.Auth;

internal static class DependencyInjection
{
    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.SectionName);
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddKeyedScoped<IExternalUserIdProvider, OnboardingExternalUserIdProvider>(AuthConstants.Schemes.Onboarding);
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, OnboardingBasicHandler>(AuthConstants.Schemes.Onboarding, null);

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                AuthConstants.Policies.RekiAppJwt,
                policy =>
                    policy
                        .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser())
            .AddPolicy(
                AuthConstants.Policies.RekiMiniAppGrant,
                policy => policy.RequireAssertion(context => context.HasGrant(AuthConstants.Grants.MiniApp)))
            .AddPolicy(
                AuthConstants.Policies.RekiTelegramLoginGrant,
                policy => policy.RequireAssertion(context => context.HasGrant(AuthConstants.Grants.TelegramLogin)))
            .AddPolicy(
                AuthConstants.Policies.RekiRefreshGrant,
                policy => policy.RequireAssertion(context => context.HasGrant(AuthConstants.Grants.Refresh)));

        services.ConfigureOptions<ConfigureOpenIddictServerOptions>();
        services.ConfigureOptions<ConfigureOpenIddictAspNetCoreOptions>();

        services.AddOpenIddict()
            .AddCore(options => options.UseMongoDb())
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("auth/external/tg/miniapp", "auth/external/tg/widget", "auth/refresh")
                    .AllowCustomFlow(AuthConstants.Grants.MiniApp)
                    .AllowCustomFlow(AuthConstants.Grants.TelegramLogin)
                    .AllowRefreshTokenFlow()
                    .UseReferenceRefreshTokens();
                options.UseAspNetCore();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
