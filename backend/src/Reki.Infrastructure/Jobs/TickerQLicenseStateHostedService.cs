using Microsoft.Extensions.Hosting;
using TickerQ.Utilities.Licensing;

namespace Reki.Infrastructure.Jobs;

internal sealed class TickerQLicenseStateHostedService(TickerQLicenseStateProvider license) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        license.Publish(TickerQLicenseState.ForCertificate(
            TickerQLicenseStatus.Active,
            isEvaluation: false,
            Guid.Empty,
            Guid.Empty,
            "Reki",
            "Synthetic",
            "Synthetic",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            1,
            string.Empty,
            string.Empty,
            "5"));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
