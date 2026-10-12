namespace Client.Models;

public static class MemberDisplay
{
    public const string DefaultColor = "#64748b";

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant(),
        };
    }

    /// <summary>
    /// Readable text color over a member color dot: black on light colors,
    /// white on dark ones (relative luminance midpoint).
    /// </summary>
    public static string ForegroundFor(string? background)
    {
        if (background is { Length: 7 } hex && hex[0] == '#'
            && int.TryParse(hex.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out var r)
            && int.TryParse(hex.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)
            && int.TryParse(hex.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return (0.299 * r + 0.587 * g + 0.114 * b) / 255 > 0.5 ? "#000000" : "#ffffff";
        }

        return "#ffffff";
    }
}
