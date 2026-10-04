using System.Reflection;

namespace Reki.Api.Bootstrapping;

internal static class OpenApiGeneration
{
    public static bool IsRunning => string.Equals(
        Assembly.GetEntryAssembly()?.GetName().Name,
        "GetDocument.Insider",
        StringComparison.Ordinal);
}