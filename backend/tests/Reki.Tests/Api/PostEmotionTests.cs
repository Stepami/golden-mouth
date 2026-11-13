using Microsoft.Extensions.Options;
using NSubstitute;
using Reki.Application.Balance;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.CreatePostCommand.Progression;
using Reki.Application.Posts.CreatePostCommand.Score;
using Reki.Application.Posts.ML;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Reki.Tests.Common;

namespace Reki.Tests.Api;

public sealed class PostEmotionTests
{
    [Test]
    [Arguments("joy", true)]
    [Arguments("surprise", true)]
    [Arguments("anger", false)]
    [Arguments("no_emotion", false)]
    public async Task Score_OnlyStrongestJoyOrSurpriseReceivesEmotionBonusAsync(string strongest, bool expected)
    {
        var classifier = Substitute.For<IClassifier>();
        var scores = new Dictionary<string, double>
        {
            ["anger"] = 0.1, ["sadness"] = 0.1, ["surprise"] = 0.1,
            ["joy"] = 0.1, ["fear"] = 0.1, ["no_emotion"] = 0.1,
        };
        scores[strongest] = 0.9;
        classifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, double>>(scores));
        var nli = Substitute.For<INliScorer>();
        nli.ScoreAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult<IReadOnlyList<NliScores>>([new(0.5, 0.5), new(0.5, 0.5)]));
        var context = new PostScoringContext();
        var analysis = new PostTextAnalysis(
            classifier,
            nli,
            Substitute.For<IRekiEmbeddingGenerator<World>>(),
            Substitute.For<IRekiEmbeddingGenerator<GamePost>>(),
            context);
        var scorer = new PostScorer(context, analysis, []);
        var world = TestGameData.CreateWorld();
        var now = DateTimeOffset.UnixEpoch;
        var post = new GamePost(PostId.New(now), world.Player.PlayerId.ToAuthorId(), world.Id, "post", 100, [], now, 0, 0, 1, false, false);

        var result = await scorer.ScoreAsync(post, world, CreateBalance(), false, CancellationToken.None);

        await Assert.That(result.PositiveEmotion).IsEqualTo(expected);
    }

    [Test]
    public async Task ViralChance_EmotionBonusRespectsDifficultyFlopOrRepostAsync()
    {
        var monitor = Substitute.For<IOptionsMonitor<GameBalance>>();
        monitor.CurrentValue.Returns(CreateBalance());
        var calculator = new PostProgressionCalculator(monitor);
        var world = TestGameData.CreateWorld();
        var player = new PlayerProfile
        {
            Id = world.Player.PlayerId,
            ExternalUserIds = [],
            BirthGroup = "teen",
            Address = Address.Nick,
            Wallet = PlayerWallet.Create(DateTimeOffset.UnixEpoch),
            Skills = new Dictionary<SkillId, SkillProgress>(),
            Settings = new PlayerSettings("on", "off", false),
        };
        var ordinary = new PostScore(0.5, 0.5, 0.5, false, null);
        var positive = ordinary with { PositiveEmotion = true };

        // The stable roll for publication 41 is about 12.18%: above 7.2% but below 13.2%.
        var withoutBonus = calculator.Calculate(player, world, ordinary, false, "41", false);
        var withBonus = calculator.Calculate(player, world, positive, false, "41", false);
        var flop = calculator.Calculate(player, world, positive, false, "41", true);
        var repost = calculator.Calculate(player, world, positive, false, "41", false, repost: true);

        await Assert.That(withoutBonus.Viral).IsFalse();
        await Assert.That(withBonus.Viral).IsTrue();
        await Assert.That(flop.Viral).IsFalse();
        await Assert.That(repost.Viral).IsFalse();
    }

    private static GameBalance CreateBalance()
    {
        var difficulty = new DifficultyBalance(
            1,
            1.2,
            0,
            1,
            1,
            0,
            new Dictionary<string, double>(),
            new Dictionary<string, double>(),
            new Dictionary<WorldDrama, DramaBalance> { [WorldDrama.Mid] = new(1, 0, 1) },
            [],
            "sad_tone");
        return new GameBalance(
            10,
            20,
            5,
            5,
            new GrowthBalance(1, 1, 0, 1, 1, 1000),
            new ViralBalance(0.03, 0.06, 3, 0.05),
            new SkillBalance([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], 1, 1, 1),
            new ThreadBalance(3, 9),
            new Dictionary<WorldDifficulty, DifficultyBalance> { [WorldDifficulty.Easy] = difficulty });
    }
}
