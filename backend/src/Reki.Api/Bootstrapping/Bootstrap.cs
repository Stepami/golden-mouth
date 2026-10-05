using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reki.Application.Game;
using Reki.Infrastructure;
using Reki.Infrastructure.Logging;
using Reki.Infrastructure.Persistence;
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
        builder.Services.AddSingleton(ReadGameBalance(builder.Configuration));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTgMiniAppAuth(builder.Configuration);
        if (OpenApiGeneration.IsRunning)
        {
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        }

        builder.Services
            .AddApplication()
            .AddInfrastructure(
                builder.Configuration,
                initializeDatabase: !OpenApiGeneration.IsRunning)
            .AddApi();
    }

    private static GameBalance ReadGameBalance(IConfiguration configuration)
    {
        IConfigurationSection posts = configuration.GetRequiredSection("posts");
        IConfigurationSection growth = configuration.GetRequiredSection("growth");
        IConfigurationSection relations = configuration.GetRequiredSection("relations");
        string resetValue = posts["reset_time_msk"]
            ?? throw new InvalidOperationException("posts.reset_time_msk is required.");
        if (!TimeOnly.TryParseExact(
            resetValue,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out TimeOnly resetTime))
        {
            throw new InvalidOperationException("posts.reset_time_msk must use HH:mm format.");
        }

        return new GameBalance(
            posts.GetValue<int>("free_per_day"),
            posts.GetValue<int>("plus_per_day"),
            resetTime,
            posts.GetValue<int>("plus_interest_bonus_once"),
            growth.GetValue<long>("start_subs"),
            relations.GetValue<int>("dm_star_replies_per_day"));
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
