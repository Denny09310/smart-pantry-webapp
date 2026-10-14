using Server.Data.Entities;

namespace Server.Tests;

public class ModelTests
{
    [Fact]
    public void HouseholdMember_HasGeneratedId_AndIsActiveByDefault()
    {
        var member = new HouseholdMember { DisplayName = "Alex" };

        Assert.False(string.IsNullOrWhiteSpace(member.Id));
        Assert.False(member.IsArchived);
        Assert.Null(member.Color);
    }

    [Fact]
    public void HouseholdMember_IdsAreUnique()
    {
        var a = new HouseholdMember { DisplayName = "Alex" };
        var b = new HouseholdMember { DisplayName = "Sam" };

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void PantryItem_MemberLink_IsOptionalByDefault()
    {
        var item = new PantryItem
        {
            Name = "Milk",
            Quantity = 1,
            Unit = "L",
            Location = "fridge",
            ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        };

        Assert.Null(item.HouseholdMemberId);
        Assert.Null(item.HouseholdMember);
        Assert.False(string.IsNullOrWhiteSpace(item.Id));
    }

    [Fact]
    public void PantryItem_ExpirationDate_UsesDateOnlySemantics()
    {
        var date = new DateOnly(2026, 10, 12);
        var item = new PantryItem
        {
            Name = "Yogurt",
            Quantity = 2,
            Unit = "pcs",
            Location = "fridge",
            ExpirationDate = date
        };

        Assert.Equal(date, item.ExpirationDate);
        Assert.Equal(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).Date, date.ToDateTime(TimeOnly.MinValue).Date);
    }
}