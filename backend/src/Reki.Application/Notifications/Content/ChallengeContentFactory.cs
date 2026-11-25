using System.Collections.Frozen;
using System.Globalization;
using GrammarSharp.Russian;

namespace Reki.Application.Notifications.Content;

internal sealed class ChallengeContentFactory :
    INotificationContentFactory<DateOnly>
{
    private static readonly FrozenDictionary<DayOfWeek, string> Zaliznyak =
        new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Monday] = "м 3a",
            [DayOfWeek.Tuesday] = "м 3a",
            [DayOfWeek.Wednesday] = "ж 1f'",
            [DayOfWeek.Thursday] = "м 3b",
            [DayOfWeek.Friday] = "ж 5a",
            [DayOfWeek.Saturday] = "ж 1a",
            [DayOfWeek.Sunday] = "с 6*a",
        }.ToFrozenDictionary();

    public Task<string> GetContent(DateOnly context)
    {
        var ruCulture = CultureInfo.GetCultureInfo("ru-RU");
        var ruDay = context.ToString("dddd", ruCulture);
        var noun = new RussianNoun(ruDay, new(Zaliznyak[context.DayOfWeek]));
        return Task.FromResult(
            $"Задания {noun.Decline(RussianCase.Genitive, plural: false)} уже в РЕКАХ! " +
            "Выполняй квесты и получай бонусы ✅!");
    }
}
