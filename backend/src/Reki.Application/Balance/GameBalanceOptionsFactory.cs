using System.Globalization;
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
        var flop = configuration.GetSection("flop");
        var skills = configuration.GetRequiredSection("skills");
        var relations = configuration.GetRequiredSection("relations");
        var challenge = configuration.GetRequiredSection("challenge");
        var thread = configuration.GetRequiredSection("thread");
        var energy = configuration.GetRequiredSection("energy");
        var postCost = energy.GetValue<int>("post_cost");
        var commentCost = energy.GetValue<int>("comment_cost");
        if (postCost <= 0 || commentCost <= 0)
        {
            throw new InvalidOperationException("Energy costs must be positive.");
        }

        var resetValue = posts["reset_time_msk"]
                         ?? throw new InvalidOperationException("posts.reset_time_msk is required.");
        if (!TimeOnly.TryParseExact(
                resetValue,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var resetTime))
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
                    item.GetValue<double>("thresholds:angry_abuse", 1),
                    item.GetValue<double>("thresholds:sad_tone", 1),
                    item.GetValue<double>("thresholds:anxious_tone", 1),
                    item.GetValue<double>("threshold_reduction_per_step:angry_abuse"),
                    item.GetValue<double>("threshold_reduction_per_step:sad_tone"),
                    item.GetValue<double>("threshold_reduction_per_step:anxious_tone"))
                {
                    Drama = item.GetSection("drama").GetChildren().ToDictionary(
                        drama => Enum.Parse<WorldDrama>(drama.Key, ignoreCase: true),
                        drama => new DramaBalance(drama.GetValue<int>("threshold_steps"), drama.GetValue<double>("loss_multiplier"))),
                });

        if (decay.Count == 0 || levelThresholds.Count != 10 ||
            difficulties.Count != 3 || Enum.GetValues<WorldDifficulty>().Any(value => !difficulties.ContainsKey(value)) ||
            difficulties.Values.Any(value => value.GrowthCoefficient <= 0 || value.FlopChance is < 0 or > 1 || value.ViralMultiplier <= 0 || value.HateFrom < 0))
        {
            throw new InvalidOperationException("The game balance configuration is incomplete.");
        }

        return new GameBalance(
            posts.GetValue<int>("free_per_day"),
            posts.GetValue<int>("plus_per_day"),
            resetTime,
            posts.GetValue<int>("invite_bonus_both"),
            posts.GetValue<int>("invite_bonus_max_per_day"),
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
                viral.GetValue<double>("x")),
            new FlopBalance(
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
            new ThreadBalance(
                thread.GetValue<int>("bonus_replies"),
                thread.GetValue<int>("subscriber_bonus"),
                commentCost),
            difficulties,
            postCost,
            configuration.GetValue<int>("access:create_world_from_scratch:subs"),
            configuration.GetValue<double>("feed:open_probability"));
    }
}
