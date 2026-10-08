using Reki.Application.Common;
using Reki.Application.Posts;
using Reki.Domain.Player;
using Reki.Tests.Common;

namespace Reki.Tests.Posts;

public sealed class PostProgressionCalculatorTests
{
    [Test]
    public async Task Calculate_EleventhDailyPost_AwardsNoExperienceAfterTenthAsync()
    {
        // Arrange
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        PlayerProfile tenthPostPlayer = TestGameData.CreatePlayer() with { PostsUsed = 9 };
        PlayerProfile eleventhPostPlayer = TestGameData.CreatePlayer() with { PostsUsed = 10 };
        GeneratedPostTurn turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        PostProgression tenth = calculator.Calculate(tenthPostPlayer, turn, false, false);
        PostProgression eleventh = calculator.Calculate(eleventhPostPlayer, turn, false, false);

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
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        GeneratedPostTurn turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        PostProgression third = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 2 },
            turn,
            false,
            false);
        PostProgression fourth = calculator.Calculate(
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
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        PlayerProfile player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        GeneratedPostTurn turn = TestGameData.CreateTurn(0.8, 0.9, 0.7);

        // Act
        PostProgression result = calculator.Calculate(player, turn, false, false);

        // Assert
        await Assert.That(result.Skills[TestGameData.Humor].Experience).IsEqualTo(1);
        await Assert.That(result.Skills[TestGameData.Brave].Experience).IsEqualTo(0.15);
        await Assert.That(result.Skills[TestGameData.Topic].Experience).IsEqualTo(0.15);
    }

    [Test]
    public async Task Calculate_OwnWords_AppliesOnePointThreeExperienceMultiplierAsync()
    {
        // Arrange
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        PlayerProfile player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        GeneratedPostTurn turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        PostProgression result = calculator.Calculate(player, turn, true, false);

        // Assert
        await Assert.That(result.Skills[TestGameData.Brave].Experience).IsEqualTo(1.3);
        await Assert.That(result.Skills[TestGameData.Humor].Experience).IsEqualTo(0.195);
    }

    [Test]
    public async Task Calculate_CompletedWant_AppliesOnePointThreeExperienceMultiplierAsync()
    {
        // Arrange
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        PlayerProfile player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        GeneratedPostTurn turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        PostProgression result = calculator.Calculate(player, turn, false, true);

        // Assert
        await Assert.That(result.Skills[TestGameData.Brave].Experience).IsEqualTo(1.3);
        await Assert.That(result.Skills[TestGameData.Topic].Experience).IsEqualTo(0.195);
    }

    [Test]
    public async Task Calculate_CombinedExperienceBonuses_MultiplyAsync()
    {
        // Arrange
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        PlayerProfile player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        GeneratedPostTurn turn = TestGameData.CreateTurn(1, 0.4, 0.2);

        // Act
        PostProgression result = calculator.Calculate(player, turn, true, true);

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
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        GeneratedPostTurn turn = TestGameData.CreateTurn();

        // Act
        long fifth = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 4 }, turn, false, false).SubscriberDelta;
        long sixth = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 5 }, turn, false, false).SubscriberDelta;
        long eleventh = calculator.Calculate(
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
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        GeneratedPostTurn turn = TestGameData.CreateTurn();

        // Act
        long belowThreshold = calculator.Calculate(
            TestGameData.CreatePlayer() with { Subscribers = 999, PostsUsed = 3 },
            turn,
            false,
            false).SubscriberDelta;
        long atThreshold = calculator.Calculate(
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
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        PlayerProfile player = TestGameData.CreatePlayer() with { PostsUsed = 3 };
        GeneratedPostTurn turn = TestGameData.CreateTurn();

        // Act
        PostProgression result = calculator.Calculate(player, turn, true, false);

        // Assert
        await Assert.That(result.SubscriberDelta).IsEqualTo(130);
        await Assert.That(result.Quality).IsEqualTo(0.5);
    }

    [Test]
    public async Task Calculate_RestedPosts_AppliesMultiplierOnlyToFirstThreePostsAsync()
    {
        // Arrange
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        GeneratedPostTurn turn = TestGameData.CreateTurn();

        // Act
        long third = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 2, RestedPostsRemaining = 1 },
            turn,
            false,
            false).SubscriberDelta;
        long fourth = calculator.Calculate(
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
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        PlayerProfile player = TestGameData.CreatePlayer() with
        {
            PostsUsed = 3,
            TotalPostsPublished = 2,
            PostsSinceViral = 2,
        };

        // Act
        PostProgression result = calculator.Calculate(
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
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        PlayerProfile player = TestGameData.CreatePlayer() with
        {
            PostsUsed = 3,
            TotalPostsPublished = 2,
            PostsSinceViral = 0,
        };

        // Act
        PostProgression result = calculator.Calculate(
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
        GameBalance balance = CreateDeterministicBalance();
        var calculator = new PostProgressionCalculator(balance);
        GeneratedPostTurn turn = TestGameData.CreateTurn();

        // Act
        bool beforeBoundary = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3, PostsSinceViral = 13 },
            turn,
            false,
            false).Viral;
        bool atBoundary = calculator.Calculate(
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
        GameBalance baseBalance = CreateDeterministicBalance();
        GameBalance balance = baseBalance with
        {
            Worlds = new Dictionary<string, WorldBalance>
            {
                [TestGameData.WorldId.Value] = new(1, 1, 1, long.MaxValue),
            },
        };
        var calculator = new PostProgressionCalculator(balance);
        GeneratedPostTurn turn = TestGameData.CreateTurn();

        // Act
        PostProgression secondFlop = calculator.Calculate(
            TestGameData.CreatePlayer() with { PostsUsed = 3, ConsecutiveFlops = 1 },
            turn,
            false,
            false);
        PostProgression preventedThird = calculator.Calculate(
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
        GameBalance balance = TestGameData.CreateBalance();
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
