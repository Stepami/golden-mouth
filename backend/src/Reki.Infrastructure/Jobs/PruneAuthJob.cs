using OpenIddict.Abstractions;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class PruneAuthJob(
    IOpenIddictTokenManager tokens,
    IOpenIddictAuthorizationManager authorizations,
    TimeProvider timeProvider) : ITickerFunction
{
    private const int RetentionDays = 14;

    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var cutoff = timeProvider.GetUtcNow().AddDays(-RetentionDays);
        await tokens.PruneAsync(cutoff, cancellationToken);
        await authorizations.PruneAsync(cutoff, cancellationToken);
    }
}
