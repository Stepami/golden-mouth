using Microsoft.Extensions.Internal;

namespace Reki.Infrastructure.Cache;

internal sealed class TimeProviderSystemClock(TimeProvider timeProvider) : ISystemClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}