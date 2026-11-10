using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Reki.Application.Auth;
using Reki.Domain.Player;
using Reki.Infrastructure.Auth;

namespace Reki.Infrastructure.Vk;

internal sealed class VkUserIdProvider(
    IHttpContextAccessor contexts,
    IVkApi api,
    IOptions<VkOptions> options) : IExternalUserIdProvider
{
    public async Task<ExternalUserId?> GetExternalUserIdOrNullAsync(CancellationToken cancellationToken)
    {
        var token = contexts.HttpContext is { } context
            ? await context.GetTokenAsync(AuthConstants.Schemes.Vk, "access_token")
            : null;
        if (token is null)
        {
            return null;
        }

        try
        {
            var info = await api.GetUserInfoAsync(options.Value.ApplicationId, token, cancellationToken);
            return info.User?.UserId is { Length: > 0 } id && id.All(char.IsAsciiDigit) && id.Any(character => character != '0')
                ? ExternalUserId.Vk(id)
                : null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
