namespace Client.Models;

public static class CosmeticCategories
{
    public const string All = "all";

    public static readonly string[] Values = ["face", "hair", "body", "hands", "perfume"];

    public static readonly (string Value, string Label, string Icon)[] Options =
    [
        ("face", "Face", "smile"),
        ("hair", "Hair", "brush"),
        ("body", "Body", "person-standing"),
        ("hands", "Hands", "hand"),
        ("perfume", "Perfume", "spray-can")
    ];

    public static readonly string[] Units = ["pcs", "bottle", "tube", "jar", "spray", "ml"];
}
