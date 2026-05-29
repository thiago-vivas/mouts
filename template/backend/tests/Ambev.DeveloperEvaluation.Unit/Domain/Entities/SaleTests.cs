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

    private static Sale NewSale() => new()
    {
        SaleNumber = "S-TEST-0001",
        SaleDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Customer = Ref("Customer"),
        Branch = Ref("Branch")
    };

    [Fact(DisplayName = "Item with 10% tier computes discount and total correctly")]
    public void Given_QuantityInTenPercentTier_When_AddItem_Then_AppliesDiscount()
    {
        var sale = NewSale();

        var item = sale.AddItem(Ref("Beer"), 5, 10m);

        // gross = 50, discount = 10% => 5, total = 45
        item.Discount.Should().Be(5m);
        item.TotalAmount.Should().Be(45m);
        sale.TotalAmount.Should().Be(45m);
    }

    [Fact(DisplayName = "Item below 4 units has no discount")]
    public void Given_QuantityBelow4_When_AddItem_Then_NoDiscount()
    {
        var sale = NewSale();

        var item = sale.AddItem(Ref("Water"), 3, 10m);

        item.Discount.Should().Be(0m);
        item.TotalAmount.Should().Be(30m);
    }

    [Fact(DisplayName = "Adding more than 20 identical units throws")]
    public void Given_QuantityAbove20_When_AddItem_Then_Throws()
    {
        var sale = NewSale();

        var act = () => sale.AddItem(Ref("Soda"), 21, 5m);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Sale total sums only active (non-cancelled) items")]
    public void Given_SaleWithItems_When_ItemCancelled_Then_TotalExcludesIt()
    {
        var sale = NewSale();
        var item1 = sale.AddItem(Ref("Beer"), 5, 10m);   // total 45
        var item2 = sale.AddItem(Ref("Water"), 2, 10m);  // total 20
        sale.TotalAmount.Should().Be(65m);

        sale.CancelItem(item2.Id);

        item2.IsCancelled.Should().BeTrue();
        sale.TotalAmount.Should().Be(45m);
    }

    [Fact(DisplayName = "Cancelling an already-cancelled item throws")]
    public void Given_CancelledItem_When_CancelAgain_Then_Throws()
    {
        var sale = NewSale();
        var item = sale.AddItem(Ref("Beer"), 5, 10m);
        sale.CancelItem(item.Id);

        var act = () => sale.CancelItem(item.Id);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Cancelling the whole sale sets the flag")]
    public void Given_Sale_When_Cancelled_Then_IsCancelledIsTrue()
    {
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);

        sale.Cancel();

        sale.IsCancelled.Should().BeTrue();
    }

    [Fact(DisplayName = "Cancelling an already-cancelled sale throws")]
    public void Given_CancelledSale_When_CancelAgain_Then_Throws()
    {
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);
        sale.Cancel();

        var act = () => sale.Cancel();

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Cancelling an unknown item throws")]
    public void Given_Sale_When_CancelUnknownItem_Then_Throws()
    {
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);

        var act = () => sale.CancelItem(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "A valid sale passes domain validation")]
    public void Given_ValidSale_When_Validate_Then_IsValid()
    {
        var sale = NewSale();
        sale.AddItem(Ref("Beer"), 5, 10m);

        sale.Validate().IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "A sale without items fails validation")]
    public void Given_SaleWithoutItems_When_Validate_Then_IsInvalid()
    {
        var sale = NewSale();

        sale.Validate().IsValid.Should().BeFalse();
    }
}
