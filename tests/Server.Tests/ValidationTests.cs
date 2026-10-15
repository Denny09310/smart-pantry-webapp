using System.ComponentModel.DataAnnotations;

using Shared.Models;

namespace Server.Tests;

/// <summary>
/// Documents the Shared transport contracts. The same attributes are enforced
/// at runtime by Minimal API validation (<c>AddValidation</c>), which rejects
/// invalid requests with a 400 response before the handler runs.
/// </summary>
public class ValidationTests
{
    private static bool IsValid(object request)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(
            request, new ValidationContext(request), results, validateAllProperties: true);
    }

    private static CreatePantryItemRequest ValidItemRequest() => new(
        "Milk", 1, "L", "fridge", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));

    [Fact]
    public void ValidPantryItemRequest_PassesValidation()
    {
        Assert.True(IsValid(ValidItemRequest()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void PantryItemRequest_NameIsRequired(string? name)
    {
        var request = ValidItemRequest() with { Name = name! };

        Assert.False(IsValid(request));
    }

    [Fact]
    public void PantryItemRequest_NegativeQuantity_Fails()
    {
        var request = ValidItemRequest() with { Quantity = -1 };

        Assert.False(IsValid(request));
    }

    private static UpdatePantryItemRequest ValidUpdateRequest() => new(
        "Milk", 1, "L", "fridge", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));

    [Fact]
    public void UpdatePantryItemRequest_Valid_Passes()
    {
        Assert.True(IsValid(ValidUpdateRequest()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UpdatePantryItemRequest_NameIsRequired(string? name)
    {
        Assert.False(IsValid(ValidUpdateRequest() with { Name = name! }));
    }

    [Fact]
    public void UpdatePantryItemRequest_NegativeQuantity_Fails()
    {
        Assert.False(IsValid(ValidUpdateRequest() with { Quantity = -1 }));
    }

    [Theory]
    [InlineData("12345678", true)]
    [InlineData("123456789012", true)]
    [InlineData("1234567890123", true)]
    [InlineData("1234567", false)]
    [InlineData("12345678901234", false)]
    [InlineData("abcdefgh", false)]
    [InlineData("", false)]
    public void LookupProductRequest_BarcodeFormat(string barcode, bool valid)
    {
        Assert.Equal(valid, IsValid(new LookupProductRequest(barcode)));
    }

    [Theory]
    [InlineData(-1, null, false)]
    [InlineData(null, 0, false)]
    [InlineData(null, 101, false)]
    [InlineData(0, 20, true)]
    [InlineData(null, null, true)]
    public void NotificationsRequest_PagingRanges(int? skip, int? take, bool valid)
    {
        Assert.Equal(valid, IsValid(new GetNotificationsRequest { Skip = skip, Take = take }));
    }

    [Fact]
    public void PantryItemRequest_OverlongUnit_Fails()
    {
        var request = ValidItemRequest() with { Unit = new string('x', 33) };

        Assert.False(IsValid(request));
    }

    [Fact]
    public void GetRequest_TakeAboveMaximum_Fails()
    {
        var request = new GetPantryItemsRequest { Take = 101 };

        Assert.False(IsValid(request));
    }

    [Fact]
    public void GetRequest_DefaultPaging_Passes()
    {
        Assert.True(IsValid(new GetPantryItemsRequest { Location = "all" }));
        Assert.True(IsValid(new GetPantryItemsRequest()));
    }

    [Fact]
    public void GetRequest_NegativeSkip_Fails()
    {
        Assert.False(IsValid(new GetPantryItemsRequest { Skip = -1 }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CreateMemberRequest_DisplayNameIsRequired(string? name)
    {
        Assert.False(IsValid(new CreateHouseholdMemberRequest(name!)));
    }

    [Fact]
    public void CreateMemberRequest_OverlongColor_Fails()
    {
        var request = new CreateHouseholdMemberRequest("Alex", new string('x', 17));

        Assert.False(IsValid(request));
    }

    [Fact]
    public void ValidMemberRequests_PassValidation()
    {
        Assert.True(IsValid(new CreateHouseholdMemberRequest("Alex", "#A3B18A")));
        Assert.True(IsValid(new UpdateHouseholdMemberRequest("Alex", null, true)));
    }
}