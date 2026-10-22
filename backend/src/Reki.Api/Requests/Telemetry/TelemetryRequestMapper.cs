using System.Text.Json;
using Reki.Application.Telemetry.ReportCommentCommand;
using Reki.Application.Telemetry.SubmitAnalyticsCommand;
using Reki.Domain.Posts;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;

namespace Reki.Api.Requests.Telemetry;

[Mapper]
internal static partial class TelemetryRequestMapper
{
    public static ReportComment ToReportComment(this ReportRequest source) => new(CommentId.From(source.CommentId), source.Reason);

    public static SubmitAnalytics ToSubmitAnalytics(this AnalyticsBatchRequest source) => new(source.ToSubmission());

    [MapProperty(nameof(AnalyticsBatchRequest.Experiments), nameof(AnalyticsSubmission.Experiments), Use = nameof(SanitizeExperiments))]
    public static partial AnalyticsSubmission ToSubmission(this AnalyticsBatchRequest source);

    public static IReadOnlyDictionary<string, string> SanitizeProperties(
        this IReadOnlyDictionary<string, JsonElement>? properties)
    {
        if (properties is null)
        {
            return new Dictionary<string, string>();
        }

        return properties
            .Where(item => item.Key.Length <= 64)
            .ToDictionary(item => item.Key, item => SanitizeElement(item.Value));
    }

    [MapProperty(nameof(AnalyticsEventRequest.Prev), nameof(AnalyticsEntry.PreviousScreen))]
    [MapProperty(nameof(AnalyticsEventRequest.Props), nameof(AnalyticsEntry.Properties), Use = nameof(SanitizeProperties))]
    private static partial AnalyticsEntry MapEvent(AnalyticsEventRequest source);

    private static SessionId MapSession(string source) => SessionId.From(source);

    private static WorldId? MapWorldId(string? source) => source is null ? null : WorldId.From(source);

    private static IReadOnlyDictionary<string, string> SanitizeExperiments(IReadOnlyDictionary<string, string> source) =>
        source.ToDictionary(item => item.Key, item => SanitizeJsonString(item.Value, 64));

    private static string SanitizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => SanitizeJsonString(element.GetString() ?? string.Empty, 64),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => string.Empty,
            _ => $"kind:{element.ValueKind}",
        };
    }

    private static string SanitizeJsonString(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : $"len:{value.Length}";
    }
}
