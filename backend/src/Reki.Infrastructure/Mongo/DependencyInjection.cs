using Kot.MongoDB.Migrations;
using Kot.MongoDB.Migrations.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Reki.Application.Common;
using Reki.Application.Events;
using Reki.Application.Invites;
using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Worlds;
using Reki.Infrastructure.Mongo.Options;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo;

internal static class DependencyInjection
{
    public static IServiceCollection AddMongo(this IServiceCollection services)
    {
        services
            .AddOptions<MongoOptions>()
            .BindConfiguration(MongoOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.ConnectionString, UriKind.Absolute, out _),
                "Mongo:ConnectionString must be an absolute MongoDB URI.")
            .ValidateOnStart();

        services.AddSingleton<MongoCompositionRoot>();
        services.AddSingleton<IGameStore, MongoGameStore>();
        services.AddSingleton<IPlayerRepository, MongoPlayerRepository>();
        services.AddSingleton<IWorldRepository, MongoWorldRepository>();
        services.AddSingleton<IPostRepository, MongoPostRepository>();
        services.AddSingleton<IPublicationRepository, MongoPublicationRepository>();
        services.AddSingleton<IInvitationRepository, MongoInvitationRepository>();
        services.AddSingleton<IDailyGameEventRepository, MongoDailyGameEventRepository>();
        services.AddSingleton<IMongoClient>(sp => sp.GetRequiredService<MongoCompositionRoot>().MongoClient);
        services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<MongoCompositionRoot>().MongoDatabase);
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb", null, ["ready"]);

        services
            .AddMongoMigrations(
                new MigrationOptions("mongodb"),
                locationConfig => locationConfig.LoadMigrationsFromAssembly(typeof(DependencyInjection).Assembly))
            .Replace(ServiceDescriptor.Singleton<IMigrator, RekiMigrator>());
        return services;
    }
}
