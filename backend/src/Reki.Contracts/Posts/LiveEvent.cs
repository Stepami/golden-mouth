using System.Text.Json.Serialization;

namespace Reki.Contracts.Posts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ViewsLiveEvent), "views")]
[JsonDerivedType(typeof(ReactionLiveEvent), "react")]
[JsonDerivedType(typeof(CommentLiveEvent), "comment")]
[JsonDerivedType(typeof(TypingLiveEvent), "typing")]
[JsonDerivedType(typeof(ResultLiveEvent), "result")]
public abstract record LiveEvent([property: JsonIgnore] LiveEventType Type);
