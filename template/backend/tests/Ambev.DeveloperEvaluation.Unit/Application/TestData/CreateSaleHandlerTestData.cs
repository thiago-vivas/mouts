using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.TestData;

/// <summary>
/// Bogus (Faker) based builders for sale-related commands, producing valid data
/// that respects the discount/quantity rules (quantities 1..20).
/// </summary>
public static class CreateSaleHandlerTestData
{
    private static readonly Faker<CreateSaleItemDto> ItemFaker = new Faker<CreateSaleItemDto>()
        .RuleFor(i => i.ProductId, f => f.Random.Guid())
        .RuleFor(i => i.ProductName, f => f.Commerce.ProductName())
        .RuleFor(i => i.Quantity, f => f.Random.Int(1, 20))
        .RuleFor(i => i.UnitPrice, f => decimal.Round(f.Random.Decimal(1, 100), 2));

    private static readonly Faker<CreateSaleCommand> CommandFaker = new Faker<CreateSaleCommand>()
        .RuleFor(c => c.SaleDate, f => f.Date.RecentOffset().UtcDateTime)
        .RuleFor(c => c.Customer, f => new ExternalReferenceDto { Id = f.Random.Guid(), Name = f.Person.FullName })
        .RuleFor(c => c.Branch, f => new ExternalReferenceDto { Id = f.Random.Guid(), Name = f.Company.CompanyName() })
        .RuleFor(c => c.Items, _ => ItemFaker.Generate(3));

    public static CreateSaleCommand GenerateValidCommand() => CommandFaker.Generate();
}
