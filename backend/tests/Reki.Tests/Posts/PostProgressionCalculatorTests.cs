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
        var tenth = calculator.Calculate(tenthPostPlayer, turn, false, false);
        var eleventh = calculator.Calculate(eleventhPostPlayer, turn, false, false);

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
            turn,
            false,
            false);
        var fourth = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3 },
            turn,
            false,
            false);

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
        var result = calculator.Calculate(player, turn, false, false);

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
        var result = calculator.Calculate(player, turn, true, false);

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
        var result = calculator.Calculate(player, turn, false, true);

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
        var result = calculator.Calculate(player, turn, true, true);

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
            TestGameData.CreatePlayer() with { PostsUsed = 4 }, turn, false, false).SubscriberDelta;
        var sixth = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 5 }, turn, false, false).SubscriberDelta;
        var eleventh = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 10 }, turn, false, false).SubscriberDelta;

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
            TestGameData.CreatePlayer() with { Subscribers = 999, PostsUsed = 3 },
            turn,
            false,
            false).SubscriberDelta;
        var atThreshold = calculator.Calculate(
            TestGameData.CreatePlayer() with { Subscribers = 1000, PostsUsed = 3 },
            turn,
            false,
            false).SubscriberDelta;

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
        var result = calculator.Calculate(player, turn, true, false);

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
            TestGameData.CreatePlayer() with { PostsUsed = 2, RestedPostsRemaining = 1 },
            turn,
            false,
            false).SubscriberDelta;
        var fourth = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3, RestedPostsRemaining = 1 },
            turn,
            false,
            false).SubscriberDelta;

        // Assert
        await Assert.That(third).IsEqualTo(200);
        await Assert.That(fourth).IsEqualTo(100);
    }

    [Test]
    public async Task Calculate_ThirdPublishedPost_AppliesGuaranteedViralMultiplierAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var player = TestGameData.CreatePlayer() with
        {
            PostsUsed = 3,
            TotalPostsPublished = 2,
            PostsSinceViral = 2,
        };

        // Act
        var result = calculator.Calculate(
            player,
            TestGameData.CreateTurn(),
            false,
            false);

        // Assert
        await Assert.That(result.Viral).IsTrue();
        await Assert.That(result.Flop).IsFalse();
        await Assert.That(result.SubscriberDelta).IsEqualTo(300);
    }

    [Test]
    public async Task Calculate_ThirdPublishedPostAfterEarlierViral_DoesNotRepeatGuaranteeAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var player = TestGameData.CreatePlayer() with
        {
            PostsUsed = 3,
            TotalPostsPublished = 2,
            PostsSinceViral = 0,
        };

        // Act
        var result = calculator.Calculate(
            player,
            TestGameData.CreateTurn(),
            false,
            false);

        // Assert
        await Assert.That(result.Viral).IsFalse();
        await Assert.That(result.SubscriberDelta).IsEqualTo(100);
    }

    [Test]
    public async Task Calculate_AtPityBoundary_GuaranteesViralOnlyOnFifteenthPostAsync()
    {
        // Arrange
        var balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var turn = TestGameData.CreateTurn();

        // Act
        var beforeBoundary = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3, PostsSinceViral = 13 },
            turn,
            false,
            false).Viral;
        var atBoundary = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3, PostsSinceViral = 14 },
            turn,
            false,
            false).Viral;

        // Assert
        await Assert.That(beforeBoundary).IsFalse();
        await Assert.That(atBoundary).IsTrue();
    }

    [Test]
    public async Task Calculate_AfterTwoConsecutiveFlops_PreventsThirdFlopAsync()
    {
        // Arrange
        var baseBalance = CreateDeterministicBalance();
        var balance = baseBalance with
        {
            Worlds = new Dictionary<string, WorldBalance>
            {
                [TestGameData.WorldId.Value] = new(1, 1, 1, long.MaxValue),
            },
        };
        var calculator = new PostProgressionCalculator(Options.Create(balance));
        var turn = TestGameData.CreateTurn();

        // Act
        var secondFlop = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3, ConsecutiveFlops = 1 },
            turn,
            false,
            false);
        var preventedThird = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3, ConsecutiveFlops = 2 },
            turn,
            false,
            false);

        // Assert
        await Assert.That(secondFlop.Flop).IsTrue();
        await Assert.That(secondFlop.SubscriberDelta).IsEqualTo(-30);
        await Assert.That(preventedThird.Flop).IsFalse();
        await Assert.That(preventedThird.SubscriberDelta).IsEqualTo(100);
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
