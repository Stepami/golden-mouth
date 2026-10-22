namespace Reki.Contracts.Common;

/// <summary>A page of API response items.</summary>
/// <typeparam name="T">The response item type.</typeparam>
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int PageNumber, bool HasNext);
