using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Reki.Application.Common;
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
                initializeDatabase: !OpenApiGeneration.IsRunning,
                initializeLlm: !OpenApiGeneration.IsRunning)
            .AddApi();
    }

    private static GameBalance ReadGameBalance(IConfiguration configuration)
    {
        IConfigurationSection posts = configuration.GetRequiredSection("posts");
        IConfigurationSection growth = configuration.GetRequiredSection("growth");
        IConfigurationSection viral = configuration.GetRequiredSection("viral");
        IConfigurationSection flop = configuration.GetRequiredSection("flop");
        IConfigurationSection skills = configuration.GetRequiredSection("skills");
        IConfigurationSection relations = configuration.GetRequiredSection("relations");
        IConfigurationSection challenge = configuration.GetRequiredSection("challenge");
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

        IReadOnlyList<PostDecayBalance> decay = growth.GetRequiredSection("decay")
            .GetChildren()
            .Select(item => new PostDecayBalance(
                item.GetValue<int>("from"),
                item.GetValue<int>("to"),
                item.GetValue<double>("x")))
            .ToArray();
        IReadOnlyList<double> levelThresholds = skills.GetRequiredSection("levels_xp")
            .GetChildren()
            .Select(item => item.Get<double>())
            .ToArray();
        IReadOnlyDictionary<string, WorldBalance> worlds = configuration
            .GetRequiredSection("worlds")
            .GetChildren()
            .ToDictionary(
                item => item.Key,
                item => new WorldBalance(
                    item.GetValue<double>("k"),
                    item.GetValue<double>("flop"),
                    item.GetValue<double>("viral_x")),
                StringComparer.Ordinal);

        if (decay.Count == 0 || levelThresholds.Count != 10 || worlds.Count == 0)
        {
            throw new InvalidOperationException("The game balance configuration is incomplete.");
        }

        return new GameBalance(
            posts.GetValue<int>("free_per_day"),
            posts.GetValue<int>("plus_per_day"),
            resetTime,
            posts.GetValue<int>("plus_interest_bonus_once"),
            posts.GetValue<int>("challenge_win_bonus_per_day"),
            posts.GetValue<int>("challenge_win_bonus_days"),
            new GrowthBalance(
                growth.GetValue<double>("power"),
                growth.GetValue<double>("k"),
                decay,
                growth.GetValue<long>("starter_below_subs"),
                growth.GetValue<double>("starter_x"),
                growth.GetValue<double>("own_words_x"),
                growth.GetValue<int>("rested_after_skip_days"),
                growth.GetValue<int>("rested_first_posts"),
                growth.GetValue<double>("rested_x"),
                growth.GetValue<long>("start_subs")),
            new ViralBalance(
                viral.GetValue<double>("base"),
                viral.GetValue<double>("per_quality"),
                viral.GetValue<int>("guaranteed_post"),
                viral.GetValue<int>("pity_posts"),
                viral.GetValue<double>("x")),
            new FlopBalance(
                flop.GetValue<int>("max_in_row"),
                flop.GetValue<double>("loss_x")),
            new SkillBalance(
                levelThresholds,
                skills.GetValue<double>("xp_per_post_main"),
                skills.GetValue<double>("xp_per_post_side"),
                skills.GetValue<int>("xp_only_first_posts_per_day"),
                skills.GetValue<double>("xp_first_posts_x"),
                skills.GetValue<int>("xp_first_posts_n"),
                skills.GetValue<double>("xp_own_words_x"),
                skills.GetValue<double>("xp_want_done_x")),
            new RelationBalance(
                relations.GetValue<int>("min"),
                relations.GetValue<int>("max"),
                relations.GetValue<int>("max_gain_per_day"),
                relations.GetValue<int>("star_card_at"),
                relations.GetValue<int>("dm_star_replies_per_day")),
            new ChallengeBalance(
                challenge.GetValue<int>("days"),
                challenge.GetValue<int>("min_posts")),
            worlds);
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
