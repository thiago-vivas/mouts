using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CreateSaleValidatorTests
{
    private readonly CreateSaleValidator _validator = new();

    private static CreateSaleCommand Valid() => new()
    {
        SaleDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Customer = new ExternalReferenceDto { Id = Guid.NewGuid(), Name = "C" },
        Branch = new ExternalReferenceDto { Id = Guid.NewGuid(), Name = "B" },
        Items = new List<CreateSaleItemDto>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "P", Quantity = 5, UnitPrice = 10m }
        }
    };

    [Fact(DisplayName = "Valid create command passes validation")]
    public void Given_ValidCommand_Then_Valid()
        // Act & Assert
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact(DisplayName = "Command without items fails")]
    public void Given_NoItems_Then_Invalid()
    {
        // Arrange
        var c = Valid();
        c.Items.Clear();
        // Act & Assert
        _validator.Validate(c).IsValid.Should().BeFalse();
    }

    [Theory(DisplayName = "Invalid item quantities fail")]
    [InlineData(0)]
    [InlineData(21)]
    public void Given_InvalidQuantity_Then_Invalid(int quantity)
    {
        // Arrange
        var c = Valid();
        c.Items[0].Quantity = quantity;
        // Act & Assert
        _validator.Validate(c).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Missing customer id fails")]
    public void Given_EmptyCustomerId_Then_Invalid()
    {
        // Arrange
        var c = Valid();
        c.Customer.Id = Guid.Empty;
        // Act & Assert
        _validator.Validate(c).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Non-positive unit price fails")]
    public void Given_ZeroUnitPrice_Then_Invalid()
    {
        // Arrange
        var c = Valid();
        c.Items[0].UnitPrice = 0m;
        // Act & Assert
        _validator.Validate(c).IsValid.Should().BeFalse();
    }
}

public class UpdateSaleValidatorTests
{
    private readonly UpdateSaleValidator _validator = new();

    private static UpdateSaleCommand Valid() => new()
    {
        Id = Guid.NewGuid(),
        SaleDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Customer = new ExternalReferenceDto { Id = Guid.NewGuid(), Name = "C" },
        Branch = new ExternalReferenceDto { Id = Guid.NewGuid(), Name = "B" },
        Items = new List<UpdateSaleItemDto>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "P", Quantity = 5, UnitPrice = 10m }
        }
    };

    [Fact(DisplayName = "Valid update command passes validation")]
    public void Given_ValidCommand_Then_Valid()
        // Act & Assert
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact(DisplayName = "Empty id fails")]
    public void Given_EmptyId_Then_Invalid()
    {
        // Arrange
        var c = Valid();
        c.Id = Guid.Empty;
        // Act & Assert
        _validator.Validate(c).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Quantity above 20 fails")]
    public void Given_QuantityAbove20_Then_Invalid()
    {
        // Arrange
        var c = Valid();
        c.Items[0].Quantity = 25;
        // Act & Assert
        _validator.Validate(c).IsValid.Should().BeFalse();
    }
}
