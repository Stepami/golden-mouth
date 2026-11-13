using Reki.Domain.Posts;

namespace Reki.Tests.Api;

public sealed class PostLikesCalculationTests
{
    [Test]
    [Arguments(0L, 2, 0L)]
    [Arguments(9L, 2, 1L)]
    [Arguments(9L, 10, 1L)]
    [Arguments(100L, 2, 2L)]
    [Arguments(100L, 10, 10L)]
    [Arguments(101L, 2, 3L)]
    public async Task CalculateLikes_RoundsUpPercentageOfViewsAsync(long views, int percentage, long expected)
    {
        await Assert.That(GamePost.CalculateLikes(views, percentage)).IsEqualTo(expected);
    }
}
