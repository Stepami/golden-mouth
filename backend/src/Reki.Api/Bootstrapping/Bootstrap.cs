using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reki.Infrastructure;
using Reki.Infrastructure.Logging;
using Reki.Infrastructure.Persistence;
using Serilog;

namespace Reki.Api.Bootstrapping;

internal static class Bootstrap
{
    public static Serilog.ILogger CreateInitialLogger()
    {
        return new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }

    public static async Task RunAsync(string[] arguments)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(arguments);
        InfrastructureMode infrastructureMode = GetInfrastructureMode(builder.Environment);

        ConfigureServices(builder, infrastructureMode);
        ConfigureLogging(builder);

        WebApplication app = builder.Build();
        ConfigurePipeline(app);
        await app.RunAsync();
    }

    private static InfrastructureMode GetInfrastructureMode(IHostEnvironment environment)
    {
        return environment.IsProduction() && !OpenApiGeneration.IsRunning
            ? InfrastructureMode.Production
            : InfrastructureMode.Local;
    }

    private static void ConfigureServices(
        WebApplicationBuilder builder,
        InfrastructureMode infrastructureMode)
    {
        builder.Services
            .AddApplication()
            .AddInfrastructure(builder.Configuration, infrastructureMode)
            .AddApi();
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(
            ConfigureLogger,
            writeToProviders: true);
    }

    private static void ConfigureLogger(
        HostBuilderContext context,
        IServiceProvider services,
        LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        if (context.HostingEnvironment.IsProduction() && !OpenApiGeneration.IsRunning)
        {
            AddMongoSink(services, loggerConfiguration);
        }
    }

    private static void AddMongoSink(
        IServiceProvider services,
        LoggerConfiguration loggerConfiguration)
    {
        IMongoClient mongoClient = services.GetRequiredService<IMongoClient>();
        MongoOptions mongoOptions = services
            .GetRequiredService<IOptions<MongoOptions>>()
            .Value;

        loggerConfiguration.WriteToMongoDb(mongoClient, mongoOptions);
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging();

        MapApiDocumentation(app);
        app.MapControllers();
    }

    private static void MapApiDocumentation(WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "swagger";
            options.SwaggerEndpoint("/openapi/v1.json", "Reki API v1");
        });
    }
}
