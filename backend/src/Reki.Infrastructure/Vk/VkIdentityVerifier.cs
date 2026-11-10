using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Vk;

public sealed class VkIdentityVerifier(IVkApi api, IOptions<VkOptions> options)
{
    public async Task<ExternalUserId?> VerifyAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await api.GetUserInfoAsync(options.Value.ApplicationId, accessToken, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var info = await response.Content.ReadFromJsonAsync(VkJsonSerializerContext.Default.VkUserInfoResponse, cancellationToken);
            return info?.User?.UserId is { Length: > 0 } id && id.All(char.IsAsciiDigit) && id.Any(character => character != '0')
                ? ExternalUserId.Vk(id)
                : null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // External failures never establish an identity. Do not log provider bodies or tokens.
            return null;
        }
    }
}
