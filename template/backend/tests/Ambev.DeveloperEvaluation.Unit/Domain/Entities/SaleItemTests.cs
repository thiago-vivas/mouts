using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleItemTests
{
    private static ExternalReference Product() => new(Guid.NewGuid(), "Product");

    [Fact(DisplayName = "New item gets a non-empty client-generated id")]
    public void Given_NewItem_Then_HasId()
    {
        new SaleItem(Product(), 1, 10m).Id.Should().NotBe(Guid.Empty);
    }

    [Fact(DisplayName = "Changing unit price recomputes discount and total")]
    public void Given_Item_When_UnitPriceChanged_Then_Recomputes()
    {
        var item = new SaleItem(Product(), 10, 10m); // 20% of 100 = 20, total 80
        item.TotalAmount.Should().Be(80m);

        item.SetUnitPrice(20m); // 20% of 200 = 40, total 160

        item.Discount.Should().Be(40m);
        item.TotalAmount.Should().Be(160m);
    }

    [Fact(DisplayName = "Cancelling an item flags it")]
    public void Given_Item_When_Cancelled_Then_Flagged()
    {
        var item = new SaleItem(Product(), 5, 10m);
        item.Cancel();
        item.IsCancelled.Should().BeTrue();
    }
}

public class ExternalReferenceTests
{
    [Fact(DisplayName = "Constructor stores id and name")]
    public void Given_IdAndName_When_Constructed_Then_Stored()
    {
        var id = Guid.NewGuid();
        var reference = new ExternalReference(id, "Acme");

        reference.Id.Should().Be(id);
        reference.Name.Should().Be("Acme");
    }

    [Fact(DisplayName = "Null name is normalized to empty string")]
    public void Given_NullName_When_Constructed_Then_Empty()
    {
        new ExternalReference(Guid.NewGuid(), null!).Name.Should().Be(string.Empty);
    }
}
