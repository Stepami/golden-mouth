using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class PruneAuthJob(
    IOpenIddictTokenManager tokens,
    IOpenIddictAuthorizationManager authorizations,
    IOptions<JobsOptions> options,
    TimeProvider timeProvider) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var cutoff = timeProvider.GetUtcNow().AddDays(-options.Value.AuthPruneAfterDays);
        await tokens.PruneAsync(cutoff, cancellationToken);
        await authorizations.PruneAsync(cutoff, cancellationToken);
    }
}
