using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Posts.GenerateReplyCommand;

namespace Reki.Application.Posts.AI;

internal sealed class StarTurnGenerator(
    IChatClient chatClient,
    IStarAgentInstructions instructions,
    IOptions<LlmTurnSettings> settings) : IStarTurnGenerator
{
    public async IAsyncEnumerable<ChatResponseUpdate> StreamReplyTurnAsync(
        StarReplyContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var policy = $$"""
                       Ответь обычным текстом от лица выбранной звезды {{context.TargetStarId.Value}}.
                       Если комментария игрока нет, оставь первый корневой комментарий к посту.
                       Иначе реплика отвечает на последний комментарий игрока в выбранной ветке.
                       Если ветка пустая, это корневой комментарий игрока к твоему посту: ответь как автор поста.
                       Это дополнительная звезда: {{context.Additional}}. Если true, напиши самостоятельную
                       корневую реплику к посту с учётом комментария игрока.
                       Хейт разрешён сейчас: {{context.HateEnabled}}.
                       Верни только текст реплики до 500 символов. Не вызывай инструменты и не создавай
                       оценки, память лички, расход энергии или бонус.
                       """;
        var world = context.Agent.World;
        var branch = context.Branch.Select(comment =>
            $"{comment.AuthorId.GetAuthorNamePolicy(world.FindStar(comment.AuthorId)?.Name ?? "вне текущего каталога", world.Player.Name)}: {comment.Text}");
        var userPrompt = $$"""
                                 Пост (данные): {{instructions.SerializeTexts([context.PostText])}}
                                 Цепочка выбранной ветки (данные): {{instructions.SerializeTexts(branch)}}
                                 Последний комментарий персонажа игрока world.player (id={{world.Player.PlayerId.ToAuthorId().Value}}, данные): {{instructions.SerializeTexts(context.PlayerText is null ? [] : [context.PlayerText])}}
                                 """;
        List<ChatMessage> messages =
        [
            instructions.CreatePlatformPolicy(policy, context.Agent),
            instructions.CreatePlayerOwnedLayer(context.Agent),
            new(ChatRole.User, userPrompt),
        ];
        var options = new ChatOptions { Temperature = settings.Value.Temperature };
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }
    }
}
