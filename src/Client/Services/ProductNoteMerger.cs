namespace Client.Services;

/// <summary>
/// Merges a scanned product brand into the notes field without accumulating
/// repetitions: re-scanning the same barcode (or a brand list Open Food Facts
/// already repeats) leaves the notes unchanged.
/// </summary>
public static class ProductNoteMerger
{
    public static string? Merge(string? existingNotes, string? brand)
    {
        var tokens = Split(existingNotes)
            .Concat(Split(brand))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return tokens.Count == 0 ? null : string.Join(", ", tokens);
    }

    private static IEnumerable<string> Split(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !string.IsNullOrWhiteSpace(t));
    }
}
