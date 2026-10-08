using System.Text.Json;
using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Contracts.Events;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;
using Reki.Contracts.Worlds;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;
using ContractSkillId = Reki.Contracts.Player.SkillId;
using ContractWorldId = Reki.Contracts.Player.WorldId;
using DomainEventId = Reki.Domain.Events.EventId;

namespace Reki.Api.Mapping;

[Mapper]
public sealed partial class ApiMapper
{
    public PlayerResponse MapPlayer(PlayerView source)
    {
        PlayerProfile player = source.Player;
        IReadOnlyDictionary<ContractSkillId, SkillResponse> skills = player.Skills.ToDictionary(
            item => Enum.Parse<ContractSkillId>(item.Key.Value, true),
            item => MapSkill(item.Value));
        var week = Enumerable.Range(0, 7)
            .Select(day => day < Math.Min(7, player.StreakDays))
            .ToArray();

        return new PlayerResponse(
            player.Id.Value,
            player.Name,
            player.Handle,
            player.Role,
            Enum.Parse<Address>(player.Address, true),
            Enum.Parse<ContractWorldId>(player.World.Value, true),
            player.Subscribers,
            source.PostsLeft,
            source.PostsCap,
            player.FirstPostInviteBonusUsed ? 0 : player.FirstPostInviteBonus,
            new StreakResponse(player.StreakDays, week, "ещё день — и бонус"),
            skills,
            player.Wants
                .Select(want => new WantResponse(want.Id, want.Text, want.Reward, want.Done))
                .ToArray(),
            player.Achievements
                .Where(item => item.WorldId == player.World)
                .Select(item => new CollectionCardResponse(
                    item.Id,
                    item.Emoji,
                    item.Title,
                    item.IsOpen))
                .ToArray(),
            string.Empty,
            "свой голос",
            null,
            new PlayerSettingsResponse(
                Enum.Parse<Drama>(player.Settings.Drama, true),
                player.Settings.Notifications,
                player.Settings.Quiet,
                player.Settings.Voice,
                player.Settings.Plus),
            new Dictionary<string, string>());
    }

    public DmResponse MapDirectMessages(DirectMessageView view)
    {
        StarPersona persona = view.Persona;
        StarResponse star = new(persona.Id, persona.Name, persona.Role, persona.Trait, persona.About, view.Relation);
        return new DmResponse(
            star,
            "на связи",
            view.Conversation.Memory,
            view.Conversation.Messages
                .Select(MapDirectMessage)
                .ToArray(),
            ["спросить про новый пост", "обсудить событие дня"],
            view.RepliesLeft);
    }

    public AnalyticsSubmission MapAnalytics(AnalyticsBatchRequest source)
    {
        return new AnalyticsSubmission(
            SessionId.From(source.Session),
            source.Platform,
            source.Experiments.ToDictionary(
                item => item.Key,
                item => SanitizeJsonString(item.Value, 64)),
            source.Events.Select(item => new AnalyticsEntry(
                item.Name,
                item.Timestamp,
                item.Screen,
                item.Prev,
                SanitizeProperties(item.Props))).ToArray());
    }

    public partial SendDirectMessageResponse MapSentDirectMessage(SentDirectMessage source);

    [MapProperty(nameof(FriendSummary.Subscribers), nameof(FriendResponse.Subs))]
    public partial FriendResponse MapFriend(FriendSummary source);

    public partial IReadOnlyList<FriendResponse> MapFriends(IReadOnlyList<FriendSummary> source);

    [MapperIgnoreSource(nameof(DailyGameEvent.PlayerId))]
    [MapperIgnoreSource(nameof(DailyGameEvent.DayStartedAtUtc))]
    [MapperIgnoreSource(nameof(DailyGameEvent.Answer))]
    public partial GameEventResponse MapEvent(DailyGameEvent source);

    [MapperIgnoreSource(nameof(WorldRules.Id))]
    [MapperIgnoreSource(nameof(WorldRules.PlayerId))]
    [MapperIgnoreSource(nameof(WorldRules.WorldId))]
    public partial WorldRulesResponse MapWorldRules(WorldRules source);

    public SaveWorldRulesCommand MapWorldRulesRequest(
        UpdateWorldRulesRequest source,
        TelegramUserId telegramUserId)
    {
        return new SaveWorldRulesCommand(
            telegramUserId,
            source.Title,
            source.Presets,
            source.Description,
            source.Rules.Select(MapWorldRule).ToArray());
    }

    public IReadOnlyDictionary<string, string> SanitizeProperties(
        IReadOnlyDictionary<string, JsonElement>? properties)
    {
        if (properties is null)
        {
            return new Dictionary<string, string>();
        }

        return properties
            .Where(item => item.Key.Length <= 64)
            .ToDictionary(item => item.Key, item => SanitizeElement(item.Value));
    }

    private static DirectMessageAuthor MapDirectMessageAuthor(string source)
    {
        return Enum.Parse<DirectMessageAuthor>(source, true);
    }

    private static GameEventKind MapGameEventKind(string source)
    {
        return Enum.Parse<GameEventKind>(source, true);
    }

    private static string MapEventId(DomainEventId source) => source.Value;

    private static string MapPlayerId(PlayerId source) => source.Value;

    private static string SanitizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => SanitizeJsonString(element.GetString() ?? string.Empty, 64),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => string.Empty,
            _ => $"kind:{element.ValueKind}",
        };
    }

    private static string SanitizeJsonString(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : $"len:{value.Length}";
    }

    [MapperIgnoreSource(nameof(SkillProgress.Experience))]
    private partial SkillResponse MapSkill(SkillProgress source);

    private partial DirectMessageResponse MapDirectMessage(DirectMessage source);

    private partial WorldRuleResponse MapWorldRule(WorldRule source);

    private partial WorldRule MapWorldRule(WorldRuleResponse source);
}
