namespace Client.Models;

public static class PantryLocations
{
    public const string All = "all";

    public static readonly string[] Values = ["fridge", "pantry", "freezer", "cabinet"];

    public static readonly (string Value, string Label, string Icon)[] Options =
    [
        ("pantry", "Pantry", "package"),
        ("fridge", "Fridge", "refrigerator"),
        ("freezer", "Freezer", "snowflake"),
        ("cabinet", "Cabinet", "archive")
    ];

    public static readonly string[] Units = ["pcs", "jar", "can", "box", "kg", "g", "ml", "litre"];
}
