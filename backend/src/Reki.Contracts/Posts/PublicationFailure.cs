namespace Reki.Contracts.Posts;

/// <summary>Safe, durable explanation. Never contains submitted text or provider output.</summary>
public sealed record PublicationFailure(string Code, string Category, string PolicyVersion);
