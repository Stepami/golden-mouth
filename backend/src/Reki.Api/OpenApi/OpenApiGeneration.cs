using System.Reflection;

namespace Reki.Api.OpenApi;

internal static class OpenApiGeneration
{
    public static bool IsRunning => string.Equals(
        Assembly.GetEntryAssembly()?.GetName().Name,
        "GetDocument.Insider",
        StringComparison.Ordinal);
}