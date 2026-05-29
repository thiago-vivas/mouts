using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

/// <summary>
/// Unit tests for the Sale aggregate: discount/total computation, recalculation,
/// item cancellation and whole-sale cancellation.
/// </summary>
public class SaleTests
{
    private static ExternalReference Ref(string name) => new(Guid.NewGuid(), name);

    private static Sale NewSale() => new(
        "S-TEST-0001",
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Ref("Customer"),
        Ref("Branch"));

    [Fact(DisplayName = "Item with 10% tier computes discount and total correctly")]
    public void Given_QuantityInTenPercentTier_When_AddItem_Then_AppliesDiscount()
    {
        // Arrange
        var sale = NewSale();

        // Act
        var item = sale.AddItem(Ref("Beer"), 5, 10m);

        // Assert
        // gross = 50, discount = 10% => 5, total = 45
        item.Discount.Should().Be(5m);
        item.TotalAmount.Should().Be(45m);
        sale.TotalAmount.Should().Be(45m);
    }

    [Fact(DisplayName = "Item below 4 units has no discount")]
    public void Given_QuantityBelow4_When_AddItem_Then_NoDiscount()
    {
        // Arrange
        var sale = NewSale();

        // Act
        var item = sale.AddItem(Ref("Water"), 3, 10m);

        // Assert
        item.Discount.Should().Be(0m);
        item.TotalAmount.Should().Be(30m);
    }

    [Fact(DisplayName = "Adding more than 20 identical units throws")]
    public void Given_QuantityAbove20_When_AddItem_Then_Throws()
    {
        // Arrange
        var sale = NewSale();

        // Act
        var act = () => sale.AddItem(Ref("Soda"), 21, 5m);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Sale total sums only active (non-cancelled) items")]
    public void Given_SaleWithItems_When_ItemCancelled_Then_TotalExcludesIt()
    {
        // Arrange
        var sale = NewSale();
        var item1 = sale.AddItem(Ref("Beer"), 5, 10m);   // total 45
        var item2 = sale.AddItem(Ref("Water"), 2, 10m);  // total 20
        sale.TotalAmount.Should().Be(65m);

        // Act
        sale.CancelItem(item2.Id);

        // Assert
        item2.IsCancelled.Should().BeTrue();
        sale.TotalAmount.Should().Be(45m);
    }

    [Fact(DisplayName = "Cancelling an already-cancelled item throws")]
    public void Given_CancelledItem_When_CancelAgain_Then_Throws()
    {
        // Arrange
        var sale = NewSale();
        var item = sale.AddItem(Ref("Beer"), 5, 10m);
        sale.CancelItem(item.Id);

        // Act
        var act = () => sale.CancelItem(item.Id);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Cancelling the whole sale sets the flag")]
    public void Given_Sale_When_Cancelled_Then_IsCancelledIsTrue()
    {
        // Arrange
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);

        // Act
        sale.Cancel();

        // Assert
        sale.IsCancelled.Should().BeTrue();
    }

    [Fact(DisplayName = "Cancelling an already-cancelled sale throws")]
    public void Given_CancelledSale_When_CancelAgain_Then_Throws()
    {
        // Arrange
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);
        sale.Cancel();

        // Act
        var act = () => sale.Cancel();

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Cancelling an unknown item throws")]
    public void Given_Sale_When_CancelUnknownItem_Then_Throws()
    {
        // Arrange
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);

        // Act
        var act = () => sale.CancelItem(Guid.NewGuid());

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "A valid sale passes domain validation")]
    public void Given_ValidSale_When_Validate_Then_IsValid()
    {
        // Arrange
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);

        // Act & Assert
        sale.Validate().IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "A sale without items fails validation")]
    public void Given_SaleWithoutItems_When_Validate_Then_IsInvalid()
    {
        // Arrange
        var sale = NewSale();

        // Act & Assert
        sale.Validate().IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Cancelling the whole sale cancels its items and zeroes the total")]
    public void Given_SaleWithItems_When_Cancelled_Then_ItemsCancelledAndTotalZero()
    {
        // Arrange
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);
        sale.AddItem(Ref("Water"), 2, 10m);

        // Act
        sale.Cancel();

        // Assert
        sale.IsCancelled.Should().BeTrue();
        sale.Items.Should().OnlyContain(i => i.IsCancelled);
        sale.TotalAmount.Should().Be(0m);
    }

    [Fact(DisplayName = "SyncItems updates a matching product line in place, preserving its id")]
    public void Given_Sale_When_SyncItemsSameProduct_Then_LineUpdatedInPlace()
    {
        // Arrange
        var sale = NewSale();
        var beer = Ref("Beer");
        var original = sale.AddItem(beer, 5, 10m);   // 10% tier => total 45

        // Act: same product id, new quantity (10 => 20% tier)
        sale.SyncItems(new[] { (beer, 10, 10m) });

        // Assert
        sale.Items.Should().HaveCount(1);
        var line = sale.Items.Single();
        line.Id.Should().Be(original.Id);            // identity preserved
        line.Quantity.Should().Be(10);
        sale.TotalAmount.Should().Be(80m);           // 100 - 20% = 80
    }

    [Fact(DisplayName = "SyncItems adds new products and removes ones no longer present")]
    public void Given_Sale_When_SyncItemsDifferentProducts_Then_AddedAndRemoved()
    {
        // Arrange
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);
        var water = Ref("Water");

        // Act: beer dropped, water added
        sale.SyncItems(new[] { (water, 2, 10m) });

        // Assert
        sale.Items.Should().HaveCount(1);
        sale.Items.Single().Product.Id.Should().Be(water.Id);
        sale.TotalAmount.Should().Be(20m);
    }

    [Fact(DisplayName = "A cancelled sale cannot be modified")]
    public void Given_CancelledSale_When_UpdateDetails_Then_Throws()
    {
        // Arrange
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);
        sale.Cancel();

        // Act
        var act = () => sale.UpdateDetails(sale.SaleDate, Ref("New"), Ref("Branch"));

        // Assert
        act.Should().Throw<DomainException>();
    }
}
