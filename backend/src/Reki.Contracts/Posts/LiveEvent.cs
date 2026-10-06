using System.Text.Json.Serialization;

namespace Reki.Contracts.Posts;

[JsonPolymorphic]
[JsonDerivedType(typeof(ViewsLiveEvent))]
[JsonDerivedType(typeof(ReactionLiveEvent))]
[JsonDerivedType(typeof(CommentLiveEvent))]
[JsonDerivedType(typeof(TypingLiveEvent))]
[JsonDerivedType(typeof(ResultLiveEvent))]
public abstract record LiveEvent(LiveEventType Type);
