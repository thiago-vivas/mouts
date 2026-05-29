using Ambev.DeveloperEvaluation.Domain.Validation;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Validation;

public class SaleValidatorTests
{
    private readonly SaleValidator _validator = new();

    [Fact(DisplayName = "A well-formed sale passes the domain validator")]
    public void Given_ValidSale_Then_Valid()
        => _validator.Validate(SaleTestData.GenerateValidSale()).IsValid.Should().BeTrue();

    [Fact(DisplayName = "A sale without items fails the domain validator")]
    public void Given_SaleWithoutItems_Then_Invalid()
    {
        var sale = SaleTestData.GenerateValidSale();
        sale.ClearItems();
        _validator.Validate(sale).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "A sale without a sale number fails")]
    public void Given_NoSaleNumber_Then_Invalid()
    {
        var sale = SaleTestData.GenerateValidSale();
        sale.SaleNumber = string.Empty;
        _validator.Validate(sale).IsValid.Should().BeFalse();
    }
}
