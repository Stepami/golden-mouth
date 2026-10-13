using Kot.MongoDB.Migrations;
using Microsoft.AspNetCore.DataProtection;
using Reki.Infrastructure;
using Reki.Infrastructure.Serialization;
using Serilog;
using TgMiniAppAuth;
using TickerQ.DependencyInjection;

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
        var builder = WebApplication.CreateBuilder(arguments);
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
        builder.Services.AddTgMiniAppAuth(builder.Configuration);
        builder.Services.AddApi();
        if (OpenApiGeneration.IsRunning)
        {
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
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
}