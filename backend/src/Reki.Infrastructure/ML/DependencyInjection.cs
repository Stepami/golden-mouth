using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reki.Application.Posts.ML;
using Reki.Infrastructure.ML.Classifier;
using Reki.Infrastructure.ML.Nli;

namespace Reki.Infrastructure.ML;

internal static class DependencyInjection
{
    public static IServiceCollection AddMlScorers(this IServiceCollection services)
    {
        services
            .AddOptions<NliOptions>()
            .BindConfiguration(NliOptions.SectionName)
            .Validate(
                options => File.Exists(options.ModelPath) && File.Exists(options.VocabPath),
                "Nli model and vocabulary files must exist.")
            .ValidateOnStart();
        services.AddSingleton<INliScorer, LocalNliScorer>();

        services
            .AddOptions<InfinityOptions>()
            .BindConfiguration(InfinityOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                           uri.Scheme is "http" or "https",
                "Infinity:BaseUrl must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();
        services.AddHttpClient<IClassifier, InfinityClassifier>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<InfinityOptions>>().Value.BaseUrl);
        });

        return services;
    }
}
