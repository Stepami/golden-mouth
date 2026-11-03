using System.Diagnostics;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;
using OpenTelemetry;

namespace Reki.Infrastructure.Auth;

public sealed class TelegramBotTelemetry : BaseProcessor<Activity>, IDownstreamDependencyMetadata
{
    private readonly string _host;

    public TelegramBotTelemetry(IOptions<TelegramOptions> telegramOptions)
    {
        _host = new Uri(telegramOptions.Value.ApiUrl).Host;
        UniqueHostNameSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { _host };
        RequestMetadata = new HashSet<RequestMetadata>
        {
            new("POST", "/bot{token}/{action}"),
            new("GET", "/bot{token}/{action}"),
        };
    }

    public string DependencyName => "TelegramBot";

    public ISet<string> UniqueHostNameSuffixes { get; }

    public ISet<RequestMetadata> RequestMetadata { get; }

    public override void OnEnd(Activity activity)
    {
        if (activity.Source.Name != "System.Net.Http" ||
            activity.GetTagItem("server.address") is not string host ||
            !string.Equals(host, _host, StringComparison.OrdinalIgnoreCase) ||
            activity.GetTagItem("url.full") is not string rawUrl)
        {
            return;
        }

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            activity.SetTag("url.full", null);
            return;
        }

        activity.SetTag("url.full", $"{uri.Scheme}://{uri.Authority}{RedactPath(uri.AbsolutePath)}");
    }

    private static string RedactPath(string path)
    {
        const string prefix = "/bot";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            return "/REDACTED";
        }

        var slash = path.IndexOf('/', prefix.Length);
        if (slash <= prefix.Length)
        {
            return "/REDACTED";
        }

        var action = path[(slash + 1)..];
        return action.Length > 0 && action.All(char.IsAsciiLetter)
            ? $"/botREDACTED/{action}"
            : "/REDACTED";
    }
}
