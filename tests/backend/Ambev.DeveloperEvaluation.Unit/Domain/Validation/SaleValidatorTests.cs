using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Validation;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Validation;

public class SaleValidatorTests
{
    private readonly SaleValidator _validator = new();

    [Fact(DisplayName = "A well-formed sale passes the domain validator")]
    public void Given_ValidSale_Then_Valid()
        // Act & Assert
        => _validator.Validate(SaleTestData.GenerateValidSale()).IsValid.Should().BeTrue();

    [Fact(DisplayName = "A sale without items fails the domain validator")]
    public void Given_SaleWithoutItems_Then_Invalid()
    {
        // Arrange
        var sale = SaleTestData.GenerateValidSale(itemCount: 0);
        // Act & Assert
        _validator.Validate(sale).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "A sale without a sale number fails")]
    public void Given_NoSaleNumber_Then_Invalid()
    {
        // Arrange
        var sale = new Sale(
            string.Empty,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new ExternalReference(Guid.NewGuid(), "Customer"),
            new ExternalReference(Guid.NewGuid(), "Branch"));
        sale.AddItem(new ExternalReference(Guid.NewGuid(), "Product"), 1, 10m);
        // Act & Assert
        _validator.Validate(sale).IsValid.Should().BeFalse();
    }
}
