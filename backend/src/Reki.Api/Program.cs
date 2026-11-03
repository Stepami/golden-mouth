using Kot.MongoDB.Migrations;
using Reki.Api.OpenApi;
using Reki.Api.Serialization;
using Reki.Application;
using Reki.Infrastructure;
using Serilog;
using TickerQ.DependencyInjection;

namespace Reki.Api;

public partial class Program
{
    protected Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var bootstrapLogger = CreateInitialLogger();
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSerilog(bootstrapLogger, dispose: true));
        var logger = loggerFactory.CreateLogger<Program>();
        try
        {
            await RunAsync(args);
        }
        catch (Exception exception)
        {
            LogServiceStopped(logger, exception);
        }
    }

    private static Serilog.ILogger CreateInitialLogger()
    {
        return new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }

    private static async Task RunAsync(string[] arguments)
    {
        var builder = WebApplication.CreateBuilder(arguments);
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        builder.Configuration.AddJsonFile(
            Path.Combine(AppContext.BaseDirectory, "config", "balance.json"),
            optional: false,
            reloadOnChange: true);

        ConfigureServices(builder);
        ConfigureLogging(builder);

        var app = builder.Build();
        if (!OpenApiGeneration.IsRunning)
        {
            ConfigurePipeline(app);
            var migrator = app.Services.GetRequiredService<IMigrator>();
            await migrator.MigrateAsync();
        }

        MapApiDocumentation(app);
        app.MapControllers();

        await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddApi();
        if (OpenApiGeneration.IsRunning)
        {
            return;
        }

        builder.Services
            .AddApplication()
            .AddInfrastructure(builder.Configuration, ApiJsonSerializerContext.Default);
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(
            ConfigureLogger,
            preserveStaticLogger: true,
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
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging();
        app.UseTickerQ();

        app.UseAuthentication();
        app.UseAuthorization();
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

    [LoggerMessage(
        EventId = 9000,
        Level = LogLevel.Critical,
        Message = "The service stopped unexpectedly")]
    private static partial void LogServiceStopped(ILogger<Program> logger, Exception exception);
}
