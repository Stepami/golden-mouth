using Microsoft.AspNetCore.DataProtection;
using Reki.Infrastructure;
using Serilog;
using TgMiniAppAuth;

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
        builder.Configuration.AddJsonFile(
            Path.Combine(AppContext.BaseDirectory, "config", "balance.json"),
            optional: false,
            reloadOnChange: true);

        ConfigureServices(builder);
        ConfigureLogging(builder);

        WebApplication app = builder.Build();
        ConfigurePipeline(app);
        await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTgMiniAppAuth(builder.Configuration);
        if (OpenApiGeneration.IsRunning)
        {
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        }

        builder.Services
            .AddApplication()
            .AddInfrastructure(
                initializeDatabase: !OpenApiGeneration.IsRunning,
                initializeLlm: !OpenApiGeneration.IsRunning)
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
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging();
        app.UseAuthentication();
        app.UseAuthorization();

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
