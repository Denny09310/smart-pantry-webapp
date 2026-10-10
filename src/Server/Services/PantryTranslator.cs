namespace Server.Services;

/// <summary>
/// Italian pantry names to TheMealDB English ingredients. TheMealDB only
/// indexes English, so Italian stock would never match without this.
/// Unknown words pass through untouched (they may already be English or
/// universal: pasta, mozzarella, pizza). Coverage is staples-first; misses
/// degrade to skipped queries, never errors.
/// </summary>
internal static class PantryTranslator
{
    private static readonly Dictionary<string, string> ItalianToEnglish = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aglio"] = "garlic",
        ["agnello"] = "lamb",
        ["albicocche"] = "apricot",
        ["albicocca"] = "apricot",
        ["alloro"] = "bay leaf",
        ["ananas"] = "pineapple",
        ["anguria"] = "watermelon",
        ["arance"] = "orange",
        ["arancia"] = "orange",
        ["arachidi"] = "peanuts",
        ["avena"] = "oats",
        ["basilico"] = "basil",
        ["birra"] = "beer",
        ["biscotti"] = "cookies",
        ["broccoli"] = "broccoli",
        ["burro"] = "butter",
        ["caffè"] = "coffee",
        ["cannella"] = "cinnamon",
        ["capperi"] = "capers",
        ["carne"] = "meat",
        ["carota"] = "carrot",
        ["carote"] = "carrot",
        ["cavolo"] = "cabbage",
        ["ceci"] = "chickpeas",
        ["cetriolo"] = "cucumber",
        ["cetrioli"] = "cucumber",
        ["ciliegie"] = "cherry",
        ["cioccolato"] = "chocolate",
        ["cipolla"] = "onion",
        ["cipolle"] = "onion",
        ["cocomero"] = "watermelon",
        ["coniglio"] = "rabbit",
        ["cozze"] = "mussels",
        ["crackers"] = "crackers",
        ["cumino"] = "cumin",
        ["curry"] = "curry powder",
        ["fagioli"] = "beans",
        ["fagiolini"] = "green beans",
        ["farina"] = "flour",
        ["fichi"] = "figs",
        ["formaggio"] = "cheese",
        ["fragole"] = "strawberry",
        ["frutta"] = "fruit",
        ["funghi"] = "mushroom",
        ["gamberi"] = "shrimp",
        ["gelato"] = "ice cream",
        ["gorgonzola"] = "gorgonzola",
        ["insalata"] = "lettuce",
        ["kiwi"] = "kiwi",
        ["lamponi"] = "raspberries",
        ["lasagne"] = "lasagne",
        ["latte"] = "milk",
        ["lattuga"] = "lettuce",
        ["lenticchie"] = "lentils",
        ["lievito"] = "yeast",
        ["limoni"] = "lemon",
        ["limone"] = "lemon",
        ["mais"] = "corn",
        ["maiale"] = "pork",
        ["marmellata"] = "jam",
        ["mascarpone"] = "mascarpone",
        ["manzo"] = "beef",
        ["mela"] = "apple",
        ["mele"] = "apple",
        ["melanzana"] = "eggplant",
        ["melanzane"] = "eggplant",
        ["melone"] = "melon",
        ["menta"] = "mint",
        ["merluzzo"] = "cod",
        ["miele"] = "honey",
        ["mirtilli"] = "blueberries",
        ["more"] = "blackberries",
        ["mozzarella"] = "mozzarella",
        ["nocciole"] = "hazelnuts",
        ["noci"] = "walnuts",
        ["noce moscata"] = "nutmeg",
        ["olio"] = "olive oil",
        ["olio d'oliva"] = "olive oil",
        ["olive"] = "olives",
        ["origano"] = "oregano",
        ["panna"] = "cream",
        ["pane"] = "bread",
        ["pangrattato"] = "breadcrumbs",
        ["paprika"] = "paprika",
        ["passata"] = "tomato",
        ["pasta"] = "pasta",
        ["patata"] = "potato",
        ["patate"] = "potato",
        ["pelati"] = "tomatoes",
        ["pepe"] = "pepper",
        ["peperone"] = "bell pepper",
        ["peperoni"] = "bell pepper",
        ["pera"] = "pear",
        ["pere"] = "pear",
        ["pesca"] = "peach",
        ["pesche"] = "peach",
        ["pesce"] = "fish",
        ["pinoli"] = "pine nuts",
        ["piselli"] = "peas",
        ["pistacchi"] = "pistachios",
        ["pollo"] = "chicken",
        ["pomodoro"] = "tomato",
        ["pomodori"] = "tomato",
        ["prezzemolo"] = "parsley",
        ["prosciutto"] = "ham",
        ["prugne"] = "plums",
        ["ricotta"] = "ricotta",
        ["riso"] = "rice",
        ["rosmarino"] = "rosemary",
        ["salmone"] = "salmon",
        ["salame"] = "salami",
        ["sale"] = "salt",
        ["salsiccia"] = "sausage",
        ["sedano"] = "celery",
        ["spinaci"] = "spinach",
        ["spaghetti"] = "spaghetti",
        ["succo"] = "juice",
        ["tacchino"] = "turkey",
        ["tè"] = "tea",
        ["timo"] = "thyme",
        ["tonno"] = "tuna",
        ["uova"] = "egg",
        ["uovo"] = "egg",
        ["uva"] = "grapes",
        ["vaniglia"] = "vanilla",
        ["verdura"] = "vegetables",
        ["vino"] = "wine",
        ["vongole"] = "clams",
        ["yogurt"] = "yogurt",
        ["zenzero"] = "ginger",
        ["zucca"] = "pumpkin",
        ["zucchero"] = "sugar",
        ["zucchine"] = "zucchini",
        ["zucchina"] = "zucchini",
    };

    /// <summary>
    /// English lookup candidates for a pantry name: the translated word
    /// plus the original (deduped), so English stock keeps working.
    /// </summary>
    public static IReadOnlyList<string> Candidates(string name)
    {
        var trimmed = name.Trim();

        if (trimmed.Length == 0)
            return [];

        if (ItalianToEnglish.TryGetValue(trimmed, out var english)
            && !string.Equals(english, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            return [english, trimmed];
        }

        return [trimmed];
    }
}
