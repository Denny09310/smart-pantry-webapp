using Client.Services;

namespace Client.Tests;

public class ProductNoteMergerTests
{
    [Fact]
    public void Merge_EmptyNotes_ReturnsBrand()
    {
        Assert.Equal("Acme", ProductNoteMerger.Merge(null, "Acme"));
        Assert.Equal("Acme", ProductNoteMerger.Merge("  ", "Acme"));
    }

    [Fact]
    public void Merge_NullBrand_KeepsNotes()
    {
        Assert.Equal("Top shelf", ProductNoteMerger.Merge("Top shelf", null));
        Assert.Null(ProductNoteMerger.Merge(null, null));
    }

    [Fact]
    public void Merge_Rescan_DoesNotRepeatBrand()
    {
        Assert.Equal("Acme", ProductNoteMerger.Merge("Acme", "Acme"));
        Assert.Equal("Acme", ProductNoteMerger.Merge("Acme", "acme"));
        Assert.Equal(
            "Top shelf, Acme",
            ProductNoteMerger.Merge("Top shelf, Acme", "Acme, acme"));
    }

    [Fact]
    public void Merge_Appends_NewBrandOnce()
    {
        Assert.Equal(
            "Top shelf, Acme, FreshFarm",
            ProductNoteMerger.Merge("Top shelf", "Acme, FreshFarm, acme"));
    }
}
