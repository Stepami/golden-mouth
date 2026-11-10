using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace Reki.Infrastructure.Yandex;

public interface IYandexApi
{
    [HttpGet("info")]
    ITask<YandexUserInfoResponse> GetUserInfoAsync(
        [Header("Authorization")] string authorization,
        CancellationToken cancellationToken);
}
