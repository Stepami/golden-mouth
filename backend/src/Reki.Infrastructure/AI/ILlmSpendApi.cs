using WebApiClientCore.Attributes;

namespace Reki.Infrastructure.AI;

/// <summary>Reads the gateway's recorded spend for one completed response.</summary>
public interface ILlmSpendApi
{
    [HttpGet("/spend/logs?request_id={requestId}")]
    [RawReturn(EnsureSuccessStatusCode = false)]
    Task<HttpResponseMessage> GetLogsAsync(
        string requestId,
        CancellationToken cancellationToken);
}
