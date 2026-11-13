using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Reki.Domain.Worlds;

namespace Reki.Application.Balance;

/// <inheritdoc/>
internal sealed class GameBalanceOptionsFactory(
    IConfiguration configuration,
    IEnumerable<IConfigureOptions<GameBalance>> setups,
    IEnumerable<IPostConfigureOptions<GameBalance>> postConfigures,
    IEnumerable<IValidateOptions<GameBalance>> validations)
    : OptionsFactory<GameBalance>(setups, postConfigures, validations)
{
    /// <summary>
    /// The JSON structure differs from the immutable GameBalance record.
    /// </summary>
    protected override GameBalance CreateInstance(string name)
    {
        var posts = configuration.GetRequiredSection("posts");
        var growth = configuration.GetRequiredSection("growth");
        var viral = configuration.GetRequiredSection("viral");
        var skills = configuration.GetRequiredSection("skills");
        var challenge = configuration.GetRequiredSection("challenge");
        var thread = configuration.GetRequiredSection("thread");
        var energy = configuration.GetRequiredSection("energy");
        var postCost = energy.GetValue<int>("post_cost");
        var commentCost = energy.GetValue<int>("comment_cost");
        if (postCost <= 0 || commentCost <= 0)
        {
            throw new InvalidOperationException("Energy costs must be positive.");
        }

        IReadOnlyList<double> levelThresholds = skills.GetRequiredSection("levels_xp")
            .GetChildren()
            .Select(item => item.Get<double>())
            .ToArray();
        IReadOnlyDictionary<WorldDifficulty, DifficultyBalance> difficulties = configuration
            .GetRequiredSection("difficulties")
            .GetChildren()
            .ToDictionary(
                item => Enum.Parse<WorldDifficulty>(item.Key, ignoreCase: true),
                item => new DifficultyBalance(
                    item.GetValue<double>("k"),
                    item.GetValue<double>("flop"),
                    item.GetValue<double>("viral_x"),
                    item.GetValue<long>("hate_from"),
                    item.GetValue<double>("loss_x"),
                    item.GetValue<double>("max_subscriber_fraction"),
                    item.GetValue<double>("topic_min_cosine"),
                    item.GetRequiredSection("thresholds").GetChildren().ToDictionary(entry => entry.Key, entry => entry.Get<double>()),
                    item.GetRequiredSection("threshold_reduction_per_step").GetChildren().ToDictionary(entry => entry.Key, entry => entry.Get<double>()),
                    item.GetRequiredSection("drama").GetChildren().ToDictionary(
                        entry => Enum.Parse<WorldDrama>(entry.Key, ignoreCase: true),
                        entry => new DramaBalance(entry.GetValue<double>("hate_x"), entry.GetValue<int>("threshold_steps"), entry.GetValue<double>("loss_multiplier"))),
                    item.GetSection("reason_priority").GetChildren().Select(entry => entry.Value ?? string.Empty).ToArray(),
                    item["emotion_tie_break"] ?? "sad_tone"));

        if (levelThresholds.Count != 10 ||
            difficulties.Count != 3 || Enum.GetValues<WorldDifficulty>().Any(value => !difficulties.ContainsKey(value)) ||
            difficulties.Values.Any(value => value.GrowthCoefficient <= 0 || value.FlopChance is < 0 or > 1 || value.ViralMultiplier <= 0 || value.HateFrom < 0))
        {
            throw new InvalidOperationException("The game balance configuration is incomplete.");
        }

        return new GameBalance(
            posts.GetValue<int>("free_per_day"),
            posts.GetValue<int>("plus_per_day"),
            posts.GetValue<int>("invite_bonus_both"),
            posts.GetValue<int>("plus_interest_bonus_once"),
            posts.GetValue<int>("challenge_win_bonus_per_day"),
            posts.GetValue<int>("challenge_win_bonus_days"),
            new GrowthBalance(
                growth.GetValue<double>("power"),
                growth.GetValue<double>("k"),
                growth.GetValue<long>("starter_below_subs"),
                growth.GetValue<double>("starter_x"),
                growth.GetValue<double>("own_words_x"),
                growth.GetValue<long>("start_subs")),
            new ViralBalance(
                viral.GetValue<double>("base"),
                viral.GetValue<double>("per_quality"),
                viral.GetValue<double>("x")),
            new FlopBalance(0),
            new SkillBalance(
                levelThresholds,
                skills.GetValue<double>("xp_per_post_main"),
                skills.GetValue<double>("xp_per_post_side"),
                skills.GetValue<double>("xp_own_words_x")),
            new ChallengeBalance(
                challenge.GetValue<int>("days"),
                challenge.GetValue<int>("min_posts")),
            new ThreadBalance(
                thread.GetValue<int>("bonus_replies"),
                thread.GetValue<int>("subscriber_bonus"),
                commentCost),
            difficulties,
            postCost,
            configuration.GetValue<int>("access:create_world_from_scratch:subs"),
            configuration.GetValue<double>("feed:open_probability"),
            energy.GetValue<int>("cap"));
    }
}
