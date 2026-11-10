using System.Security.Claims;
using AspNetCore.Authentication.Basic;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Reki.Application.Auth;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Auth;

internal static class DependencyInjection
{
    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.SectionName);
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddKeyedScoped<IExternalUserIdProvider, OnboardingExternalUserIdProvider>(AuthConstants.Schemes.Onboarding);
        services.AddAuthentication().AddBasic(AuthConstants.Schemes.Onboarding, options =>
        {
            options.SuppressWWWAuthenticateHeader = true;
            options.Events.OnValidateCredentials = context =>
            {
                if (ExternalUserId.TryFrom($"{context.Username}:{context.Password}", out var externalId))
                {
                    context.ValidationSucceeded([new Claim(AuthConstants.Claims.ExternalUserId, externalId.Value)]);
                }
                else
                {
                    context.ValidationFailed("Invalid external identity.");
                }

                return Task.CompletedTask;
            };
        });

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
