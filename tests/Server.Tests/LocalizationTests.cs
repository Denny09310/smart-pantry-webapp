using System.Collections;
using System.Globalization;
using System.Resources;

using Microsoft.Extensions.Localization;

using Shared.Resources;

namespace Server.Tests;

/// <summary>
/// Guards the string catalog: every key exists in English and Italian, every
/// accessor resolves, and the shared notification formatter speaks both.
/// </summary>
public sealed class LocalizationTests
{
    private static readonly ResourceManager Manager =
        new("Shared.Resources.UIStrings", typeof(UIStrings).Assembly);

    private static HashSet<string> KeysFor(CultureInfo culture)
    {
        var set = Manager.GetResourceSet(culture, true, false);
        Assert.NotNull(set);

        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (DictionaryEntry entry in set)
        {
            if (entry.Key is string key)
                keys.Add(key);
        }

        return keys;
    }

    private sealed class TestLocalizer(CultureInfo culture) : IStringLocalizer
    {
        public LocalizedString this[string name]
            => new(name, UIStrings.Get(name, culture), resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(culture, UIStrings.Get(name, culture), arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => throw new NotSupportedException();
    }

    [Fact]
    public void Resources_Have_Parity_Between_English_And_Italian()
    {
        var english = KeysFor(CultureInfo.InvariantCulture);
        var italian = KeysFor(new CultureInfo("it"));

        Assert.NotEmpty(english);
        Assert.Subset(new HashSet<string>(english), italian);
        Assert.Subset(new HashSet<string>(italian), english);
    }

    [Fact]
    public void Accessors_Resolve_In_Both_Languages()
    {
        var properties = typeof(UIStrings)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string))
            .ToList();

        Assert.NotEmpty(properties);

        foreach (var culture in new[] { CultureInfo.InvariantCulture, new CultureInfo("it") })
        {
            foreach (var property in properties)
            {
                var value = UIStrings.Get(property.Name, culture);
                Assert.False(string.IsNullOrWhiteSpace(value));
            }
        }
    }

    [Theory]
    [InlineData("en", "Milk expired yesterday.")]
    [InlineData("it", "Milk è scaduto ieri.")]
    public void NotificationText_Formats_Yesterday(string language, string expected)
    {
        var today = new DateOnly(2026, 10, 10);
        var localizer = new TestLocalizer(new CultureInfo(language));

        Assert.Equal(expected, NotificationText.Format("Milk", today.AddDays(-1), today, localizer));
    }

    [Theory]
    [InlineData("en", "Milk expires tomorrow.")]
    [InlineData("it", "Milk scade domani.")]
    public void NotificationText_Formats_Tomorrow(string language, string expected)
    {
        var today = new DateOnly(2026, 10, 10);
        var localizer = new TestLocalizer(new CultureInfo(language));

        Assert.Equal(expected, NotificationText.Format("Milk", today.AddDays(1), today, localizer));
    }

    [Theory]
    [InlineData("en", "Milk expires in 3 days.")]
    [InlineData("it", "Milk scade tra 3 giorni.")]
    public void NotificationText_Formats_Future(string language, string expected)
    {
        var today = new DateOnly(2026, 10, 10);
        var localizer = new TestLocalizer(new CultureInfo(language));

        Assert.Equal(expected, NotificationText.Format("Milk", today.AddDays(3), today, localizer));
    }
}
