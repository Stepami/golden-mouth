using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Options;

namespace Reki.Infrastructure.WebPush;

internal sealed class RekiPushServiceClient : PushServiceClient
{
    public RekiPushServiceClient(HttpClient httpClient, IOptions<VapidKeyOptions> options)
        : base(httpClient)
    {
        DefaultAuthentication = new VapidAuthentication(
            options.Value.PublicKey,
            options.Value.PrivateKey);
    }
}