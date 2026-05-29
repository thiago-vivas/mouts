using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Services;

/// <summary>
/// Unit tests covering the quantity-based discount business rules, including the
/// tier boundaries (4/5/9/10/20/21) and the max-20 restriction. The discount
/// applies *above* 4 items, so quantity 4 gets no discount.
/// </summary>
public class DiscountPolicyTests
{
    [Theory(DisplayName = "Quantities of 4 or fewer get no discount")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Given_Quantity4OrFewer_When_GetRate_Then_ReturnsZero(int quantity)
    {
        // Act & Assert
        DiscountPolicy.GetDiscountRate(quantity).Should().Be(0m);
    }

    [Theory(DisplayName = "Quantities from 5 to 9 get 10% discount")]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    public void Given_Quantity5To9_When_GetRate_Then_Returns10Percent(int quantity)
    {
        // Act & Assert
        DiscountPolicy.GetDiscountRate(quantity).Should().Be(0.10m);
    }

    [Theory(DisplayName = "Quantities from 10 to 20 get 20% discount")]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void Given_Quantity10To20_When_GetRate_Then_Returns20Percent(int quantity)
    {
        // Act & Assert
        DiscountPolicy.GetDiscountRate(quantity).Should().Be(0.20m);
    }

    [Theory(DisplayName = "Quantities above 20 are rejected")]
    [InlineData(21)]
    [InlineData(50)]
    public void Given_QuantityAbove20_When_GetRate_Then_Throws(int quantity)
    {
        // Act
        var act = () => DiscountPolicy.GetDiscountRate(quantity);
        // Assert
        act.Should().Throw<DomainException>().WithMessage("*more than 20*");
    }

    [Theory(DisplayName = "Quantities below 1 are rejected")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Given_QuantityBelow1_When_GetRate_Then_Throws(int quantity)
    {
        // Act
        var act = () => DiscountPolicy.GetDiscountRate(quantity);
        // Assert
        act.Should().Throw<DomainException>();
    }
}
