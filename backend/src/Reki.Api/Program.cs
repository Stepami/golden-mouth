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
        using var loggerFactory = LoggerFactory.Create(
            builder => builder.AddSerilog(
                new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger(),
                dispose: true));
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

        builder.Services.AddApi();
        if (!OpenApiGeneration.IsRunning)
        {
            builder.Services.AddHostedService<RekiStartupHostedService>();
            builder.Services
                .AddApplication()
                .AddInfrastructure(builder.Configuration, ApiJsonSerializerContext.Default);
        }

        builder.Host.UseSerilog(
            (context, services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(),
            preserveStaticLogger: true,
            writeToProviders: true);

        var app = builder.Build();
        if (!OpenApiGeneration.IsRunning)
        {
            app.UseExceptionHandler();
            app.UseStatusCodePages();
            app.UseSerilogRequestLogging();
            app.UseTickerQ();

            app.UseAuthentication();
            app.UseAuthorization();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/openapi/v1.json", "Reki API v1");
            });
        }

        app.MapControllers();

        await app.RunAsync();
    }

    [LoggerMessage(
        EventId = 9000,
        Level = LogLevel.Critical,
        Message = "The service stopped unexpectedly")]
    private static partial void LogServiceStopped(ILogger<Program> logger, Exception exception);
}
