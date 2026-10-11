using Reki.Contracts.Player;
using Reki.Contracts.Worlds;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class WorldsControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task SaveOwnWorld_WithoutLockedSafetyRule_RestoresRuleAsync()
    {
        using var api = Factory.CreateApiClient(1011);
        await OnboardAsync(api);
        var request = new UpdateWorldRulesRequest
        {
            Title = "Мой мир",
            Presets = [],
            Description = "Безопасное описание мира",
            Rules = [new WorldRuleResponse("Звёзды любят короткие посты")],
        };

        await api.Worlds.SaveOwnWorldAsync(request, CancellationToken.None);
        var rules = await api.Worlds.GetWorldRulesAsync(
            WorldId.Own,
            CancellationToken.None);

        var safetyRule = rules.Rules.Single(rule => rule.Locked);
        await Assert.That(safetyRule.Text).IsEqualTo("Без тем 18+ и травли по внешности");
    }

    [Test]
    public async Task GetReadyWorldRules_ExposesWorldDataPlusExactLockedSafetyRuleAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(1014);
        await OnboardAsync(api);

        // Act
        var rules = await api.Worlds.GetWorldRulesAsync(
            WorldId.Stage,
            CancellationToken.None);

        // Assert
        var visibleRules = string.Join("\n", rules.Rules.Select(rule => rule.Text));
        await Assert.That(visibleRules).Contains("Звёзды:");
        await Assert.That(visibleRules).Contains("Фанаты:");
        await Assert.That(visibleRules).Contains("Тон:");
        await Assert.That(visibleRules).Contains("События:");
        var safetyRule = rules.Rules.Single(rule =>
            rule.Text == "Без тем 18+ и травли по внешности");
        await Assert.That(safetyRule.Locked).IsTrue();
    }
}