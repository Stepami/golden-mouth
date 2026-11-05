using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reki.Application.Posts.AI;
using Reki.Infrastructure.AI.Embedding;

namespace Reki.Infrastructure.ML;

internal static class DependencyInjection
{
    public static IServiceCollection AddMlScorers(this IServiceCollection services)
    {
        services
            .AddOptions<NliOptions>()
            .BindConfiguration(NliOptions.SectionName)
            .Validate(
                options => !options.Enabled ||
                           (File.Exists(options.ModelPath) && File.Exists(options.VocabPath)),
                "Nli model and vocabulary files must exist when enabled.")
            .ValidateOnStart();
        services.AddSingleton<LocalNliScorer>();
        services.AddSingleton<INliScorer>(sp => sp.GetRequiredService<LocalNliScorer>());
        services.AddHealthChecks().AddCheck<LocalNliScorer>("nli", null, ["ready"]);

        services.AddHttpClient<ITextSignalsScorer, InfinityClassifierClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<EmbeddingOptions>>().Value.BaseUrl);
        });

        return services;
    }
}
