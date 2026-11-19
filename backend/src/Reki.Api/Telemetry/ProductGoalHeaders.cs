namespace Reki.Api.Telemetry;

internal static class ProductGoalHeaders
{
    public static void Write(HttpResponse response, IReadOnlyList<string> goals)
    {
        if (goals.Count > 0)
        {
            response.Headers["X-Reki-Metrika-Goals"] = string.Join(',', goals);
        }
    }
}
