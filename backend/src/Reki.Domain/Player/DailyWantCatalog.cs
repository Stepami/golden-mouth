namespace Reki.Domain.Player;

public static class DailyWantCatalog
{
    public static IReadOnlyList<DailyWant> Create()
    {
        return
        [
            new("honest", "Расскажи о своей ошибке", "опыт ×1,3", false),
            new("funny", "Добавь в пост шутку", "опыт ×1,3", false),
            new("timely", "Напиши о событии дня", "опыт ×1,3", false),
        ];
    }
}
