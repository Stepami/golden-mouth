using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Posts;
using Reki.Tests.Common;

namespace Reki.Tests.Posts;

public sealed class PostProgressionCalculatorTests
{
    [Test]
    public async Task Calculate_EleventhDailyPost_AwardsNoExperienceAfterTenthAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var tenthPostPlayer = TestGameData.CreatePlayer() with { PostsUsed = 9 };
        var eleventhPostPlayer = TestGameData.CreatePlayer() with { PostsUsed = 10 };
        var turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        var tenth = calculator.Calculate(tenthPostPlayer, TestGameData.CreateWorld(), turn, false, false, "publication-test");
        var eleventh = calculator.Calculate(eleventhPostPlayer, TestGameData.CreateWorld(), turn, false, false, "publication-test");

        // Assert
        await Assert.That(tenth.Skills[TestGameData.Brave].Experience).IsEqualTo(1);
        await Assert.That(tenth.Skills[TestGameData.Humor].Experience).IsEqualTo(0.15);
        await Assert.That(eleventh.Skills[TestGameData.Brave].Experience).IsEqualTo(0);
        await Assert.That(eleventh.Skills[TestGameData.Humor].Experience).IsEqualTo(0);
    }

    [Test]
    public async Task Calculate_FourthDailyPost_DropsFirstThreeExperienceBoostAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        var third = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 2 },
            TestGameData.CreateWorld(),
            turn,
            false,
            false,
            "publication-test");
        var fourth = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3 },
            TestGameData.CreateWorld(),
            turn,
            false,
            false,
            "publication-test");

        // Assert
        await Assert.That(third.Skills[TestGameData.Brave].Experience).IsEqualTo(2);
        await Assert.That(third.Skills[TestGameData.Humor].Experience).IsEqualTo(0.3);
        await Assert.That(fourth.Skills[TestGameData.Brave].Experience).IsEqualTo(1);
        await Assert.That(fourth.Skills[TestGameData.Humor].Experience).IsEqualTo(0.15);
    }

    [Test]
    public async Task Calculate_HumorIsMainSkill_AwardsFullExperienceWithFifteenPercentSidesAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        var turn = TestGameData.CreateTurn(0.8, 0.9, 0.7);

        // Act
        var result = calculator.Calculate(player, TestGameData.CreateWorld(), turn, false, false, "publication-test");

        // Assert
        await Assert.That(result.Skills[TestGameData.Humor].Experience).IsEqualTo(1);
        await Assert.That(result.Skills[TestGameData.Brave].Experience).IsEqualTo(0.15);
        await Assert.That(result.Skills[TestGameData.Topic].Experience).IsEqualTo(0.15);
    }

    [Test]
    public async Task Calculate_OwnWords_AppliesOnePointThreeExperienceMultiplierAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        var turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        var result = calculator.Calculate(player, TestGameData.CreateWorld(), turn, true, false, "publication-test");

        // Assert
        await Assert.That(result.Skills[TestGameData.Brave].Experience).IsEqualTo(1.3);
        await Assert.That(result.Skills[TestGameData.Humor].Experience).IsEqualTo(0.195);
    }

    [Test]
    public async Task Calculate_CompletedWant_AppliesOnePointThreeExperienceMultiplierAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        var turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        var result = calculator.Calculate(player, TestGameData.CreateWorld(), turn, false, true, "publication-test");

        // Assert
        await Assert.That(result.Skills[TestGameData.Brave].Experience).IsEqualTo(1.3);
        await Assert.That(result.Skills[TestGameData.Topic].Experience).IsEqualTo(0.195);
    }

    [Test]
    public async Task Calculate_CombinedExperienceBonuses_MultiplyAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        var turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        var result = calculator.Calculate(player, TestGameData.CreateWorld(), turn, true, true, "publication-test");

        // Assert
        await Assert.That(Math.Round(result.Skills[TestGameData.Brave].Experience, 4))
            .IsEqualTo(1.69);
        await Assert.That(Math.Round(result.Skills[TestGameData.Topic].Experience, 4))
            .IsEqualTo(0.2535);
    }

    [Test]
    public async Task Calculate_DecayBandBoundaries_UsesConfiguredMultipliersAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var turn = TestGameData.CreateTurn();

        // Act
        var fifth = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 4 },
            TestGameData.CreateWorld(),
            turn,
            false,
            false,
            "publication-test").SubscriberDelta;
        var sixth = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 5 },
            TestGameData.CreateWorld(),
            turn,
            false,
            false,
            "publication-test").SubscriberDelta;
        var eleventh = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 10 },
            TestGameData.CreateWorld(),
            turn,
            false,
            false,
            "publication-test").SubscriberDelta;

        // Assert
        await Assert.That(fifth).IsEqualTo(100);
        await Assert.That(sixth).IsEqualTo(70);
        await Assert.That(eleventh).IsEqualTo(25);
    }

    [Test]
    public async Task Calculate_AroundStarterBoundary_AppliesStarterMultiplierOnlyBelowThresholdAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var turn = TestGameData.CreateTurn();

        // Act
        var belowThreshold = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3 },
            TestGameData.CreateWorld() with
            {
                Player = TestGameData.CreateWorld().Player with
                {
                    Subscribers = 999,
                },
            },
            turn,
            false,
            false,
            "publication-test").SubscriberDelta;
        var atThreshold = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3 },
            TestGameData.CreateWorld() with
            {
                Player = TestGameData.CreateWorld().Player with
                {
                    Subscribers = 1000,
                },
            },
            turn,
            false,
            false,
            "publication-test").SubscriberDelta;

        // Assert
        await Assert.That(belowThreshold).IsEqualTo(200);
        await Assert.That(atThreshold).IsEqualTo(100);
    }

    [Test]
    public async Task Calculate_OwnWords_AppliesConfiguredGrowthMultiplierAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        var turn = TestGameData.CreateTurn();

        // Act
        var result = calculator.Calculate(player, TestGameData.CreateWorld(), turn, true, false, "publication-test");

        // Assert
        await Assert.That(result.SubscriberDelta).IsEqualTo(130);
        await Assert.That(result.Quality).IsEqualTo(0.5);
    }

    [Test]
    public async Task Calculate_RestedPosts_AppliesMultiplierOnlyToFirstThreePostsAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var turn = TestGameData.CreateTurn();

        // Act
        var third = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 2 },
            TestGameData.CreateWorld() with
            {
                Player = TestGameData.CreateWorld().Player with
                {
                    RestedPostsRemaining = 1,
                },
            },
            turn,
            false,
            false,
            "publication-test").SubscriberDelta;
        var fourth = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3 },
            TestGameData.CreateWorld() with
            {
                Player = TestGameData.CreateWorld().Player with
                {
                    RestedPostsRemaining = 1,
                },
            },
            turn,
            false,
            false,
            "publication-test").SubscriberDelta;

        // Assert
        await Assert.That(third).IsEqualTo(200);
        await Assert.That(fourth).IsEqualTo(100);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Calculate_RepeatedPublications_UsesOnlyConfiguredProbabilitiesAsync(bool viral)
    {
        var baseline = CreateDeterministicBalance();
        var balance = baseline with
        {
            Growth = baseline.Growth with { StarterMultiplier = 1 },
            Viral = baseline.Viral with { BaseChance = viral ? 1 : 0 },
            Worlds = new Dictionary<string, WorldBalance>
            {
                ["stage"] = new(1, 1, 1, long.MaxValue),
            },
        };
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var player = TestGameData.CreatePlayer();
        var world = TestGameData.CreateWorld();
        for (var index = 0; index < 20; index++)
        {
            var publicationId = $"probability-{index}";
            var result = calculator.Calculate(player, world, TestGameData.CreateTurn(), false, false, publicationId);
            var repeated = calculator.Calculate(player, world, TestGameData.CreateTurn(), false, false, publicationId);
            await Assert.That(result.Viral).IsEqualTo(viral);
            await Assert.That(result.Flop).IsEqualTo(!viral);
            await Assert.That(result.SubscriberDelta).IsEqualTo(viral ? 300L : -30L);
            await Assert.That(repeated.Viral).IsEqualTo(result.Viral);
            await Assert.That(repeated.Flop).IsEqualTo(result.Flop);
            world = world.PublishPost(result.SubscriberDelta, result.Viral, result.Skills, null, TestGameData.StarId, TestGameData.PeriodStart.AddMinutes(index), -5, 5, 1, 3);
        }
    }

    private static GameBalance CreateDeterministicBalance()
    {
        var balance = TestGameData.CreateBalance();
        return balance with
        {
            Growth = balance.Growth with
            {
                Power = 0,
                Coefficient = 100,
            },
            Viral = balance.Viral with
            {
                BaseChance = 0,
                ChancePerQuality = 0,
            },
            Worlds = new Dictionary<string, WorldBalance>
            {
                [TestGameData.WorldId.Value] = new(1, 0, 1, long.MaxValue),
            },
        };
    }
}