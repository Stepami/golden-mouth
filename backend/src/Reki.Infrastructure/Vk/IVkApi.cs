using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace Reki.Infrastructure.Vk;

public interface IVkApi
{
    [HttpPost("oauth2/user_info")]
    ITask<VkUserInfoResponse> GetUserInfoAsync(
        [AliasAs("client_id")] long clientId,
        [FormContent, AliasAs("access_token")] string accessToken,
        CancellationToken cancellationToken);
}
