namespace Client.Models;

/// <summary>One page of a paged list: items plus the total across all pages.</summary>
public sealed record Page<T>(IReadOnlyList<T> Items, int Total);
