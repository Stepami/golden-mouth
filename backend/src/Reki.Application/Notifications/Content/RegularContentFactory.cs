using GrammarSharp.Russian;
using Reki.Application.Posts;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Notifications.Content;

internal sealed class RegularContentFactory(
    IWorldPlayerCache worldPlayerCache,
    IPostRepository postRepository) : INotificationContentFactory<PlayerProfile>
{
    private static readonly RussianNoun Subscriber = new("подписчик", new("мо 3a"));
    private static readonly RussianNoun View = new("просмотр", new("м 1a"));
    private static readonly RussianNoun Like = new("лайк", new("м 3a"));

    public async Task<string> GetContent(PlayerProfile context)
    {
        var player = context;
        var worlds = await worldPlayerCache.GetWorldsAsync(player.Id, default);
        if (worlds.Count is 0)
        {
            return "РЕКИ ждут твоих постов! Скорее заходи играть!";
        }

        var worldId = worlds.Keys.ElementAt(Random.Shared.Next(worlds.Count));
        var world = worlds[worldId];
        var posts = await postRepository.GetPostsAsync(
            worldId,
            [player.Id.ToAuthorId()],
            null,
            1,
            null,
            CancellationToken.None);
        if (posts.Items is not [{ } post])
        {
            var subscribers = FormatCount(world.Player.Subscribers, Subscriber);
            return $"Так держать 🚀! Продолжай залетать в РЕКИ! Твой канал в \"{world.Interface.Title}\" уже набрал {subscribers}!";
        }

        var views = FormatCount(post.Views, View);
        var likes = FormatCount(post.Likes, Like);
        return post switch
        {
            { Virality: PostViralityStatus.Viral or PostViralityStatus.ViralCard } =>
                $"ВАУ 😱! Твой пост залетел в мире \"{world.Interface.Title}\"! Заходи в РЕКИ - залетай ещё!",
            _ => (player.Address.Equals(Address.She) ? "Раздала " : "Раздал ") +
                 $"Сваги 🤙! Твой пост набрал {views} и {likes}! РЕКИ ждут твоих новых постов!",
        };
    }

    private static string FormatCount(long count, RussianNoun noun)
    {
        var ending = count % 100 is >= 11 and <= 14 ? 0 : count % 10;
        var word = ending switch
        {
            1 => noun.Decline(RussianCase.Accusative, plural: false),
            2 or 3 or 4 => noun.Decline(
                RussianCase.Genitive,
                plural: count <= 4 && noun.Info.Properties.IsAnimate),
            _ => noun.Decline(RussianCase.Genitive, plural: true),
        };

        return $"{count} {word}";
    }
}