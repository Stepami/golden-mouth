namespace Reki.Application.Exceptions;

public sealed class MetricsUnavailableException : Exception
{
    public MetricsUnavailableException()
        : base("Verified daily metrics are unavailable for the requested range.")
    {
    }
}
