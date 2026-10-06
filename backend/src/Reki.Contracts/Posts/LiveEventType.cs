namespace Reki.Contracts.Posts;

public enum LiveEventType
{
    [System.Text.Json.Serialization.JsonStringEnumMemberName("views")]
    Views,
    [System.Text.Json.Serialization.JsonStringEnumMemberName("react")]
    React,
    [System.Text.Json.Serialization.JsonStringEnumMemberName("comment")]
    Comment,
    [System.Text.Json.Serialization.JsonStringEnumMemberName("typing")]
    Typing,
    [System.Text.Json.Serialization.JsonStringEnumMemberName("result")]
    Result,
}

