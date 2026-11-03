using Microsoft.Extensions.Options;
using OpenIddict.Server.AspNetCore;

namespace Reki.Api.Auth;

internal sealed class ConfigureOpenIddictAspNetCoreOptions(IHostEnvironment environment)
    : IConfigureOptions<OpenIddictServerAspNetCoreOptions>
{
    public void Configure(OpenIddictServerAspNetCoreOptions options)
    {
        options.EnableTokenEndpointPassthrough = true;
        options.DisableTransportSecurityRequirement = environment.IsDevelopment();
    }
}
