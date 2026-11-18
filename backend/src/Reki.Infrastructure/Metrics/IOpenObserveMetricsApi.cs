using WebApiClientCore;
using WebApiClientCore.Attributes;

namespace Reki.Infrastructure.Metrics;

public interface IOpenObserveMetricsApi
{
    [HttpPost("api/{organization}/_search?type=logs")]
    ITask<OpenObserveMetricsResponse> SearchAsync(
        string organization,
        [Header("Authorization")] string authorization,
        [JsonContent] OpenObserveMetricsRequest request,
        CancellationToken cancellationToken);
}
